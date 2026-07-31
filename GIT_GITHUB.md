# Git et GitHub — commandes et historique conseillé

## 1. Initialiser Git

```powershell
git init
git branch -M main
git status
```

## 2. Premier commit

```powershell
git add .gitignore JwtSecurityApi.csproj
git commit -m "chore: initialize ASP.NET Core 9 API project"
```

## 3. Commits pédagogiques par fonctionnalité

L'historique idéal reproduit les étapes de construction :

```powershell
git add Data Models appsettings.json
git commit -m "feat: configure EF Core and SQL Server persistence"

git add Dtos/Auth Services/DbSeeder.cs
git commit -m "feat: add user registration and secure password hashing"

git add Options Services/IJwtTokenService.cs Services/JwtTokenService.cs Program.cs
git commit -m "feat: issue and validate JWT bearer access tokens"

git add Constants Controllers/AuthController.cs Controllers/ProductsController.cs Controllers/AdminController.cs
git commit -m "feat: enforce role and policy based authorization"

git add Properties JwtSecurityApi.http README.md COURSE_COMPLET.md
git commit -m "docs: add Swagger and guided security course"

git add .github/workflows/dotnet.yml
git commit -m "ci: build the API with GitHub Actions"
```

Si tous les fichiers ont déjà été créés avant le premier commit, utilisez simplement :

```powershell
git add .
git commit -m "feat: build complete ASP.NET Core 9 JWT security API"
```

## 4. Créer le dépôt avec GitHub CLI

Installer GitHub CLI, puis :

```powershell
gh auth login
gh repo create dotnet9-jwt-security-api --public --source=. --remote=origin --push
```

Pour un dépôt privé :

```powershell
gh repo create dotnet9-jwt-security-api --private --source=. --remote=origin --push
```

## 5. Méthode via le site GitHub

Créez un dépôt vide sans README, sans `.gitignore` et sans licence, puis :

```powershell
git remote add origin https://github.com/VOTRE-COMPTE/dotnet9-jwt-security-api.git
git push -u origin main
```

## 6. Travail quotidien avec branches

```powershell
git switch -c feature/refresh-token
# modifications
git add .
git commit -m "feat: add refresh token rotation"
git push -u origin feature/refresh-token
```

Ensuite, ouvrez une Pull Request vers `main`.

## 7. Commandes de diagnostic

```powershell
git status
git log --oneline --graph --decorate --all
git diff
git diff --staged
git remote -v
git branch -a
```

## 8. Règles de sécurité Git

- Ne commitez jamais `secrets.json`, une clé JWT, un mot de passe SQL ou un token GitHub.
- Si un secret a été commité, le supprimer du dernier commit ne suffit pas : considérez-le compromis et remplacez-le immédiatement.
- Les migrations EF Core doivent être versionnées.
- Les dossiers `bin` et `obj` ne doivent pas être versionnés.
