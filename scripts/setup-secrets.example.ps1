# Exécuter depuis la racine du projet.
# Remplacez toutes les valeurs d'exemple avant l'exécution.

$jwtKey = "REMPLACEZ_PAR_UN_SECRET_ALEATOIRE_D_AU_MOINS_32_OCTETS"
$sqlConnection = "Server=(localdb)\MSSQLLocalDB;Database=JwtSecurityDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
$adminEmail = "admin@example.com"
$adminPassword = "REMPLACEZ_PAR_UN_MOT_DE_PASSE_FORT"

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" $sqlConnection
dotnet user-secrets set "Jwt:Key" $jwtKey
dotnet user-secrets set "SeedAdmin:Email" $adminEmail
dotnet user-secrets set "SeedAdmin:Password" $adminPassword

dotnet user-secrets list
