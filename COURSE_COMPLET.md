# Cours complet — JWT, authentification et autorisation avec ASP.NET Core 9, SQL Server et Swagger

## 1. Objectifs du projet

À la fin du cours, vous saurez construire une API REST capable de :

1. inscrire un utilisateur en stockant un **hachage** du mot de passe dans SQL Server ;
2. authentifier l'utilisateur avec son e-mail et son mot de passe ;
3. générer un access token JWT signé ;
4. valider automatiquement le JWT reçu dans l'en-tête HTTP ;
5. protéger des contrôleurs avec `[Authorize]` ;
6. limiter certaines opérations au rôle `Admin` ;
7. tester les routes protégées directement dans Swagger UI ;
8. gérer les migrations EF Core ;
9. versionner proprement le projet dans Git et GitHub.

Le domaine choisi est volontairement simple : des utilisateurs et des produits. Tout utilisateur authentifié peut lire les produits, mais seul un administrateur peut les créer, modifier ou supprimer.

---

## 2. Correction du vocabulaire

On parle ici de **.NET 9** et d'**ASP.NET Core 9**. Le produit historique « .NET Framework » est une autre plateforme, principalement liée aux anciennes applications Windows. Une API moderne ciblant `net9.0` utilise ASP.NET Core.

---

# Partie I — Théorie de la sécurité

## 3. Authentification et autorisation

### 3.1 Authentification

L'authentification répond à la question :

> Qui êtes-vous ?

Exemple : l'utilisateur envoie `user@example.com` et son mot de passe. L'API cherche le compte, vérifie le mot de passe et construit une identité si la vérification réussit.

Dans ASP.NET Core, l'authentification est assurée par un **schéma d'authentification**. Dans ce projet, le schéma par défaut est `Bearer`. Le gestionnaire JWT lit le token, vérifie sa signature et construit `HttpContext.User`.

### 3.2 Autorisation

L'autorisation répond à la question :

> Cette identité a-t-elle le droit d'effectuer cette action ?

Exemples :

- `[Authorize]` : toute identité authentifiée est acceptée ;
- `[Authorize(Roles = "Admin")]` : l'utilisateur doit posséder le rôle Admin ;
- `[Authorize(Policy = "AdminOnly")]` : l'utilisateur doit satisfaire une politique nommée.

### 3.3 Différence entre 401 et 403

- **401 Unauthorized** signifie en pratique « non authentifié » : token absent, invalide ou expiré.
- **403 Forbidden** signifie « authentifié mais non autorisé » : un utilisateur `User` essaie par exemple de créer un produit réservé à `Admin`.

Cette distinction est importante lors du débogage d'un frontend Angular, React ou Flutter.

---

## 4. Qu'est-ce qu'un JWT ?

JWT signifie **JSON Web Token**. Un token est généralement composé de trois parties séparées par des points :

```text
HEADER.PAYLOAD.SIGNATURE
```

### 4.1 Header

Le header décrit notamment l'algorithme de signature :

```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

### 4.2 Payload

Le payload contient des **claims**, c'est-à-dire des informations déclarées au sujet de l'utilisateur ou du token :

```json
{
  "sub": "identifiant-utilisateur",
  "email": "user@example.com",
  "unique_name": "Khalil User",
  "role": "User",
  "jti": "identifiant-unique-du-token",
  "iss": "JwtSecurityApi",
  "aud": "JwtSecurityApi.Client",
  "exp": 1234567890
}
```

Claims utilisés dans le projet :

- `sub` : identifiant stable de l'utilisateur ;
- `jti` : identifiant unique du token ;
- `email` : adresse e-mail ;
- `unique_name` : nom d'affichage ;
- `role` : rôle d'autorisation ;
- `iss` : émetteur du token ;
- `aud` : destinataire prévu ;
- `exp` : date d'expiration.

### 4.3 Signature

Avec HMAC-SHA256, la signature est calculée avec une clé secrète connue de l'API. Si quelqu'un modifie le rôle dans le payload, la signature ne correspond plus et l'API rejette le token.

### 4.4 Un JWT signé n'est pas chiffré

Le payload d'un JWT classique est encodé, pas chiffré. Il ne faut donc pas y stocker :

- mot de passe ;
- numéro de carte ;
- secret métier ;
- information confidentielle inutile au contrôle d'accès.

Le JWT prouve l'intégrité et l'origine des claims ; il ne garantit pas leur confidentialité.

---

## 5. Flux complet de connexion

```text
Client                         API                         SQL Server
  |                             |                              |
  | POST /api/auth/login        |                              |
  | email + mot de passe        |                              |
  |---------------------------->| SELECT utilisateur           |
  |                             |----------------------------->|
  |                             |<-----------------------------|
  |                             | vérifie le hash              |
  |                             | génère et signe le JWT       |
  |<----------------------------|                              |
  | accessToken                 |                              |
  |                             |                              |
  | GET /api/products           |                              |
  | Authorization: Bearer JWT   |                              |
  |---------------------------->| valide signature/iss/aud/exp|
  |                             | construit HttpContext.User   |
  |                             | applique [Authorize]         |
  |<----------------------------| réponse                      |
```

Le mot de passe n'est utilisé que pendant la connexion. Les requêtes suivantes transmettent le token.

---

## 6. Access token et refresh token

Le projet pédagogique implémente uniquement un **access token** à durée courte.

Un système de production peut ajouter un refresh token :

- l'access token expire rapidement ;
- le refresh token possède une durée plus longue ;
- il est stocké côté serveur, haché en base ;
- il peut être révoqué et renouvelé par rotation.

Ne transformez pas un access token en token de plusieurs jours uniquement pour éviter d'implémenter le renouvellement. Plus il vit longtemps, plus le vol du token est dangereux.

---

## 7. Sécurité des mots de passe

### 7.1 Ne jamais chiffrer ou stocker le mot de passe en clair

Un mot de passe doit être **haché** avec un algorithme adapté aux mots de passe. Le hachage est à sens unique : on ne « déchiffre » pas le mot de passe.

Le projet utilise `PasswordHasher<AppUser>`, fourni par ASP.NET Core Identity. À l'inscription :

```csharp
user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
```

À la connexion :

```csharp
var result = passwordHasher.VerifyHashedPassword(
    user,
    user.PasswordHash,
    request.Password);
```

L'API compare le mot de passe soumis au hash enregistré sans récupérer le mot de passe original.

### 7.2 Message d'erreur générique

Le login retourne toujours « E-mail ou mot de passe incorrect » et non :

- « utilisateur introuvable » ;
- « mot de passe incorrect ».

Cela réduit la capacité d'un attaquant à énumérer les comptes existants.

---

## 8. Rôles, claims et politiques

### 8.1 Rôle dans le JWT

Le service de token ajoute :

```csharp
new Claim("role", user.Role)
```

La validation configure :

```csharp
RoleClaimType = "role"
```

ASP.NET Core peut alors appliquer :

```csharp
[Authorize(Roles = Roles.Admin)]
```

### 8.2 Pourquoi le DTO Register n'a pas de propriété Role

Un mauvais endpoint accepterait :

```json
{
  "email": "attacker@example.com",
  "password": "Password123",
  "role": "Admin"
}
```

L'attaquant deviendrait administrateur. Dans ce projet, le serveur force :

```csharp
Role = Roles.User
```

La promotion vers Admin doit être une opération séparée, strictement protégée et auditée.

### 8.3 Politiques

Une politique est plus expressive qu'un rôle simple. Elle peut exiger :

- un rôle ;
- un claim ;
- une valeur minimale ;
- une condition métier personnalisée.

Le projet déclare `AdminOnly` puis l'utilise sur `AdminController`.

---

# Partie II — Création guidée du projet

## 9. Prérequis

Installez :

- .NET SDK 9 ;
- SQL Server LocalDB, SQL Server Express ou SQL Server Developer ;
- SQL Server Management Studio, facultatif mais utile ;
- Git ;
- GitHub CLI `gh`, facultatif ;
- Visual Studio 2022 récent, Rider ou VS Code avec l'extension C# Dev Kit.

Vérifications :

```powershell
dotnet --version
git --version
gh --version
```

---

## 10. Créer le projet

```powershell
mkdir JwtSecurityCourse
cd JwtSecurityCourse

dotnet new webapi -n JwtSecurityApi --use-controllers -f net9.0
cd JwtSecurityApi
```

Supprimez les fichiers WeatherForecast générés par le modèle s'ils existent.

### 10.1 Installer les packages

```powershell
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.18
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.18
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.18
dotnet add package Swashbuckle.AspNetCore --version 9.0.6
```

Installer l'outil EF Core :

```powershell
dotnet tool install --global dotnet-ef --version 9.0.18
```

S'il existe déjà :

```powershell
dotnet tool update --global dotnet-ef --version 9.0.18
```

Vérifier :

```powershell
dotnet ef --version
```

---

## 11. Architecture du projet

```text
JwtSecurityApi/
├── Constants/
│   ├── Policies.cs
│   └── Roles.cs
├── Controllers/
│   ├── AdminController.cs
│   ├── AuthController.cs
│   └── ProductsController.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Dtos/
│   ├── Auth/
│   └── Products/
├── Models/
│   ├── AppUser.cs
│   └── Product.cs
├── Options/
│   └── JwtOptions.cs
├── Services/
│   ├── DbSeeder.cs
│   ├── IJwtTokenService.cs
│   └── JwtTokenService.cs
├── Program.cs
├── appsettings.json
└── JwtSecurityApi.http
```

Responsabilités :

- `Models` : entités persistées dans SQL Server ;
- `Dtos` : contrats d'entrée/sortie de l'API ;
- `Data` : EF Core et mapping relationnel ;
- `Services` : hachage, token, initialisation ;
- `Controllers` : endpoints HTTP ;
- `Options` : configuration JWT typée ;
- `Constants` : noms de rôles et politiques sans chaînes dispersées.

---

## 12. Configurer SQL Server sans exposer les secrets

Initialiser User Secrets :

```powershell
dotnet user-secrets init
```

### 12.1 SQL Server LocalDB

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=JwtSecurityDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

### 12.2 SQL Server Express

Adaptez le nom d'instance :

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=JwtSecurityDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

### 12.3 SQL Server avec utilisateur SQL

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=JwtSecurityDb;User Id=api_user;Password=VOTRE_MOT_DE_PASSE;Encrypt=True;TrustServerCertificate=True"
```

En production, utilisez un compte SQL dédié avec les permissions minimales. Évitez le compte `sa` pour l'application.

---

## 13. Configurer le secret JWT

La clé HMAC doit être longue et aléatoire. Exemple PowerShell pour générer 64 octets :

```powershell
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$key = [Convert]::ToBase64String($bytes)
$key
```

Enregistrez-la ensuite :

```powershell
dotnet user-secrets set "Jwt:Key" "COLLEZ_LA_CLE_GENEREE"
```

Configurer l'administrateur initial :

```powershell
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com"
dotnet user-secrets set "SeedAdmin:Password" "Admin123!ChangeMe"
```

Afficher les noms et valeurs de développement :

```powershell
dotnet user-secrets list
```

Attention : User Secrets évite le commit Git accidentel, mais ce n'est pas un coffre de production. En production, utilisez un gestionnaire de secrets ou des variables protégées de la plateforme.

---

## 14. Entités et DbContext

### 14.1 AppUser

L'entité contient :

- un `Guid` comme identifiant ;
- le nom ;
- l'e-mail original ;
- l'e-mail normalisé pour la recherche et l'unicité ;
- le hash du mot de passe ;
- le rôle ;
- la date de création UTC.

La base ne stocke jamais le mot de passe en clair.

### 14.2 Index unique

Dans `ApplicationDbContext` :

```csharp
entity.HasIndex(user => user.NormalizedEmail).IsUnique();
```

La vérification applicative améliore le message d'erreur, mais l'index unique garantit l'intégrité même en cas de requêtes concurrentes.

### 14.3 Precision des décimaux

```csharp
entity.Property(product => product.Price).HasPrecision(18, 2);
```

Cela évite de laisser SQL Server choisir implicitement un type numérique inadapté.

---

## 15. Créer et appliquer les migrations

Première migration :

```powershell
dotnet ef migrations add InitialCreate
```

Contrôler les fichiers générés dans `Migrations/`, puis appliquer :

```powershell
dotnet ef database update
```

Commandes utiles :

```powershell
# Lister les migrations
dotnet ef migrations list

# Retirer la dernière migration non appliquée
dotnet ef migrations remove

# Générer un script SQL relisible
dotnet ef migrations script --idempotent -o migration.sql

# Revenir à une migration précise
dotnet ef database update NomMigrationPrecedente

# Revenir avant toute migration
dotnet ef database update 0
```

Les migrations font partie du code source et doivent être commitées.

---

## 16. Configuration JWT dans Program.cs

### 16.1 AddAuthentication

```csharp
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
```

- `DefaultAuthenticateScheme` indique comment construire l'identité ;
- `DefaultChallengeScheme` indique comment répondre lorsqu'une ressource exige une authentification.

### 16.2 Validation complète

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = signingKey,
    ValidateIssuer = true,
    ValidIssuer = jwtOptions.Issuer,
    ValidateAudience = true,
    ValidAudience = jwtOptions.Audience,
    ValidateLifetime = true,
    RequireExpirationTime = true,
    ClockSkew = TimeSpan.FromSeconds(30),
    NameClaimType = "unique_name",
    RoleClaimType = "role"
};
```

Chaque contrôle a une fonction :

- signature : empêche la modification du token ;
- issuer : confirme quel système a émis le token ;
- audience : confirme pour quelle API/client il a été créé ;
- lifetime : rejette un token expiré ou pas encore valide ;
- role claim : permet à `[Authorize(Roles = ...)]` de fonctionner.

### 16.3 Ordre du middleware

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

L'autorisation a besoin de l'identité construite par l'authentification. Inverser l'ordre produit des comportements incorrects.

---

## 17. Génération du JWT

Le service `JwtTokenService` évite de placer toute la logique cryptographique dans le contrôleur.

Étapes :

1. calculer la date d'émission et d'expiration ;
2. construire les claims ;
3. construire la clé de signature ;
4. signer avec HMAC-SHA256 ;
5. sérialiser le token.

Extrait :

```csharp
var credentials = new SigningCredentials(
    key,
    SecurityAlgorithms.HmacSha256);

var token = new JwtSecurityToken(
    issuer: _jwtOptions.Issuer,
    audience: _jwtOptions.Audience,
    claims: claims,
    notBefore: issuedAtUtc,
    expires: expiresAtUtc,
    signingCredentials: credentials);
```

Le service retourne également `ExpiresAtUtc`, utile au frontend pour anticiper l'expiration.

---

## 18. Endpoint Register

Route :

```http
POST /api/auth/register
```

Corps :

```json
{
  "fullName": "Khalil User",
  "email": "user@example.com",
  "password": "User123!"
}
```

Traitement :

1. validation automatique grâce à `[ApiController]` et aux annotations ;
2. normalisation de l'e-mail ;
3. vérification de l'unicité ;
4. création avec rôle `User` imposé par le serveur ;
5. hachage du mot de passe ;
6. insertion SQL Server ;
7. émission du JWT.

Réponse :

```json
{
  "accessToken": "eyJ...",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-07-29T12:00:00Z",
  "user": {
    "id": "...",
    "fullName": "Khalil User",
    "email": "user@example.com",
    "role": "User",
    "createdAtUtc": "..."
  }
}
```

---

## 19. Endpoint Login

Route :

```http
POST /api/auth/login
```

Le contrôleur recherche l'utilisateur par e-mail normalisé puis appelle `VerifyHashedPassword`.

Trois résultats sont possibles :

- `Failed` : réponse 401 ;
- `Success` : JWT ;
- `SuccessRehashNeeded` : le hash est recalculé avec les paramètres actuels, puis JWT.

Cette dernière possibilité facilite l'évolution future des paramètres de hachage.

---

## 20. Endpoint Me et lecture des claims

Route protégée :

```csharp
[Authorize]
[HttpGet("me")]
```

Le contrôleur lit `sub` :

```csharp
var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
```

Il recharge ensuite l'utilisateur depuis SQL Server. Cette approche garantit que les informations renvoyées sont actuelles.

Remarque : le rôle contenu dans un access token ne change pas rétroactivement. Si un utilisateur est promu ou bloqué, les anciens tokens restent valides jusqu'à expiration, sauf système supplémentaire de révocation ou contrôle en base.

---

## 21. Autorisation sur ProductsController

Le contrôleur entier porte :

```csharp
[Authorize]
```

Ainsi, les méthodes GET exigent un token valide.

Les mutations portent en plus :

```csharp
[Authorize(Roles = Roles.Admin)]
```

Résultats attendus :

| Situation | Résultat |
|---|---:|
| Aucun token sur GET | 401 |
| Token User sur GET | 200 |
| Token User sur POST | 403 |
| Token Admin sur POST | 201 |
| Token expiré | 401 |
| Token modifié manuellement | 401 |

---

## 22. Swagger avec JWT

Dans .NET 9, Swashbuckle n'est pas nécessairement ajouté par le modèle, donc le package est installé explicitement.

La définition de sécurité utilise le schéma HTTP Bearer :

```csharp
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT"
});
```

L'exigence de sécurité affiche le bouton **Authorize**.

### Test dans Swagger

1. lancez `dotnet run` ;
2. ouvrez `/swagger` ;
3. exécutez `/api/auth/login` ;
4. copiez `accessToken` ;
5. cliquez sur **Authorize** ;
6. collez uniquement le token dans cette configuration ;
7. testez `/api/auth/me` ;
8. testez un POST produit avec User puis Admin.

Le code désactive Swagger hors environnement Development. En production, laissez-le désactivé ou protégez-le explicitement.

---

## 23. Créer l'administrateur initial

`DbSeeder` lit :

- `SeedAdmin:Email` ;
- `SeedAdmin:Password`.

S'ils sont présents et qu'aucun compte correspondant n'existe, il crée un utilisateur `Admin` avec mot de passe haché.

Après la première connexion, changez le mot de passe d'exemple. Dans une vraie application, ajoutez un workflow sécurisé de changement et réinitialisation du mot de passe.

---

## 24. Tester avec le fichier HTTP

Le fichier `JwtSecurityApi.http` peut être exécuté dans Visual Studio ou VS Code avec une extension REST adaptée.

Après le login :

1. copiez le token ;
2. remplacez `COLLER_LE_JWT_ICI` ;
3. exécutez les requêtes protégées.

En ligne de commande avec curl :

```powershell
curl.exe -k https://localhost:7117/api/auth/me `
  -H "Authorization: Bearer VOTRE_TOKEN"
```

---

## 25. Scénario de validation complet

### Cas A — utilisateur normal

1. inscription ;
2. réponse 201 avec token ;
3. GET produits : 200 ;
4. POST produit : 403.

### Cas B — administrateur

1. configurer les secrets SeedAdmin ;
2. lancer l'application ;
3. login admin ;
4. POST produit : 201 ;
5. PUT : 200 ;
6. DELETE : 204 ;
7. GET `/api/admin/users` : 200.

### Cas C — token invalide

1. copier le token dans un décodeur local ;
2. modifier `role` sans recalculer la signature ;
3. envoyer le token : 401.

### Cas D — token absent

Envoyer GET produits sans `Authorization` : 401.

---

# Partie III — Git, GitHub et qualité du projet

## 26. Initialiser Git

```powershell
git init
git branch -M main
git status
```

La racine contient déjà un `.gitignore` adapté à .NET.

Premier commit :

```powershell
git add .
git commit -m "feat: build ASP.NET Core 9 JWT security API"
```

Pour un cours pas à pas, il est préférable de créer plusieurs commits cohérents. Le fichier `GIT_GITHUB.md` donne une séquence complète.

---

## 27. Créer le dépôt GitHub

Avec GitHub CLI :

```powershell
gh auth login
gh repo create dotnet9-jwt-security-api --public --source=. --remote=origin --push
```

Dépôt privé :

```powershell
gh repo create dotnet9-jwt-security-api --private --source=. --remote=origin --push
```

Vérifier :

```powershell
git remote -v
git log --oneline --graph --decorate
```

---

## 28. Stratégie de branches

Pour un petit projet :

- `main` : version stable ;
- `feature/nom-fonctionnalite` : développement isolé ;
- Pull Request avant fusion.

Exemple :

```powershell
git switch -c feature/refresh-token
# coder
git add .
git commit -m "feat: add refresh token rotation"
git push -u origin feature/refresh-token
```

---

## 29. GitHub Actions

Le workflow `.github/workflows/dotnet.yml` :

1. récupère le dépôt ;
2. installe .NET 9 ;
3. exécute `dotnet restore` ;
4. compile en Release.

Il ne se connecte pas à SQL Server, car il ne lance pas l'application. Pour des tests d'intégration futurs, vous pourrez ajouter un service SQL Server dans le workflow et injecter les secrets de test.

---

# Partie IV — Points de production

## 30. Ce qui manque volontairement à cet exemple pédagogique

Pour une application de production, ajoutez selon le besoin :

- refresh tokens avec rotation et révocation ;
- confirmation d'e-mail ;
- réinitialisation de mot de passe ;
- verrouillage temporaire après plusieurs échecs ;
- MFA ;
- journal d'audit ;
- rate limiting sur register/login ;
- gestion centralisée des erreurs ;
- observabilité ;
- révocation ou version de sécurité utilisateur ;
- gestion des clés avec rotation ;
- tests unitaires et d'intégration ;
- fournisseur d'identité standard lorsque l'architecture l'exige.

---

## 31. Erreurs fréquentes

### 31.1 401 avec un token qui semble valide

Vérifiez :

- préfixe `Bearer` ;
- token non expiré ;
- même clé à l'émission et à la validation ;
- issuer identique ;
- audience identique ;
- horloge du serveur ;
- bon port et bon environnement.

### 31.2 403 pour Admin

Vérifiez :

- claim `role` présent ;
- valeur exactement `Admin` ;
- `RoleClaimType = "role"` ;
- token renouvelé après une promotion ;
- casse du nom du rôle.

### 31.3 Erreur SQL Server

Vérifiez :

```powershell
dotnet user-secrets list
dotnet ef database update --verbose
```

Contrôlez l'instance : LocalDB, `SQLEXPRESS`, port 1433, authentification Windows ou SQL.

### 31.4 Swagger n'affiche pas Authorize

Vérifiez :

- `AddSecurityDefinition` ;
- `AddSecurityRequirement` ;
- `UseSwagger` ;
- `UseSwaggerUI` ;
- environnement Development.

---

## 32. Exercices proposés

1. Ajouter une route Admin permettant de promouvoir un utilisateur, sans accepter le rôle dans Register.
2. Ajouter une politique `MinimumAccountAge` basée sur un claim.
3. Ajouter une entité `RefreshToken` stockant uniquement un hash du token.
4. Ajouter un endpoint logout qui révoque le refresh token.
5. Ajouter un rate limiter sur `/api/auth/login`.
6. Ajouter des tests d'intégration vérifiant 401, 403, 200 et 201.
7. Ajouter une pagination à GET produits.
8. Ajouter un champ `IsActive` et refuser les comptes désactivés.
9. Générer un script idempotent des migrations pour le déploiement.
10. Ajouter un frontend Angular qui stocke l'access token en mémoire et utilise un interceptor HTTP.

---

## 33. Résumé mental

```text
Register
  -> valider DTO
  -> imposer rôle User
  -> hasher le mot de passe
  -> sauvegarder SQL Server
  -> générer JWT

Login
  -> trouver le compte
  -> vérifier le hash
  -> générer JWT

Requête protégée
  -> lire Authorization: Bearer <token>
  -> vérifier signature + issuer + audience + expiration
  -> construire HttpContext.User
  -> appliquer [Authorize], rôle ou politique
  -> exécuter le contrôleur ou retourner 401/403
```

Le principe fondamental est que le frontend ne décide jamais des permissions. Il peut masquer un bouton pour l'expérience utilisateur, mais l'API doit toujours refaire le contrôle d'autorisation.
