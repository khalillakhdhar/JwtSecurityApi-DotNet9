# JwtSecurityApi-DotNet9

[![.NET build](https://github.com/khalillakhdhar/JwtSecurityApi-DotNet9/actions/workflows/dotnet.yml/badge.svg)](https://github.com/khalillakhdhar/JwtSecurityApi-DotNet9/actions/workflows/dotnet.yml)
![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)
![EF Core](https://img.shields.io/badge/EF%20Core-9.0.18-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Express%2FLocalDB-CC2927)

A minimal but production-shaped **ASP.NET Core 9** Web API demonstrating **JWT authentication**, **role/policy-based authorization**, **EF Core + SQL Server persistence**, and **Swagger with Bearer auth** — built as a hands-on security course.

Domain: users register/login, every authenticated user can read `products`, only users with the `Admin` role can create/update/delete them.

> 📘 A full, deeper French-language walkthrough with theory (JWT internals, 401 vs 403, password hashing, etc.) lives in [`COURSE_COMPLET.md`](COURSE_COMPLET.md). Git workflow reference: [`GIT_GITHUB.md`](GIT_GITHUB.md). Security checklist: [`SECURITY_CHECKLIST.md`](SECURITY_CHECKLIST.md). This README is the English quick-start **and** step-by-step "build it yourself" tutorial.

---

## Table of contents

- [Features](#features)
- [Tech stack / libraries](#tech-stack--libraries)
- [Project structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Quick start (run the existing code)](#quick-start-run-the-existing-code)
- [Install dependencies — all PowerShell commands](#install-dependencies--all-powershell-commands)
- [Step-by-step: recreate the project](#step-by-step-recreate-the-project)
- [Tutorial: recreate this project from scratch](#tutorial-recreate-this-project-from-scratch)
  1. [Create the project](#1-create-the-project)
  2. [Install NuGet packages and the EF Core tool](#2-install-nuget-packages-and-the-ef-core-tool)
  3. [Project folders & constants](#3-project-folders--constants)
  4. [Domain models](#4-domain-models)
  5. [DbContext](#5-dbcontext)
  6. [DTOs](#6-dtos)
  7. [JWT options & token service](#7-jwt-options--token-service)
  8. [Configuration & secrets](#8-configuration--secrets)
  9. [Program.cs — wiring everything together](#9-programcs--wiring-everything-together)
  10. [Controllers](#10-controllers)
  11. [DbSeeder — bootstrap the first admin](#11-dbseeder--bootstrap-the-first-admin)
  12. [Create and apply the EF Core migration](#12-create-and-apply-the-ef-core-migration)
  13. [Run and test](#13-run-and-test)
- [API reference](#api-reference)
- [Configuration keys](#configuration-keys)
- [Testing the endpoints](#testing-the-endpoints)
- [Troubleshooting](#troubleshooting)
- [What's intentionally out of scope](#whats-intentionally-out-of-scope)

---

## Features

- Register / login with **hashed passwords** (`PasswordHasher<AppUser>` from ASP.NET Core Identity, no plaintext ever stored).
- **JWT Bearer** access tokens (HMAC-SHA256), signature/issuer/audience/lifetime fully validated.
- **Role-based** (`[Authorize(Roles = "Admin")]`) and **policy-based** (`AdminOnly` policy) authorization.
- Server-side role enforcement — the client can never self-assign a role at registration.
- **EF Core 9 + SQL Server** persistence with a versioned migration.
- **Swagger UI** with a working "Authorize" (Bearer) button, enabled only in Development.
- Secrets kept out of source control via **.NET User Secrets** in development.
- Auto-seeded initial `Admin` account when `SeedAdmin:*` configuration is present.
- GitHub Actions CI that restores and builds the project on every push/PR.

## Tech stack / libraries

| Package | Version | Purpose |
|---|---|---|
| [`Microsoft.AspNetCore.Authentication.JwtBearer`](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer) | 9.0.18 | Validates the `Authorization: Bearer <token>` header, builds `HttpContext.User` from the JWT claims. |
| [`Microsoft.EntityFrameworkCore.SqlServer`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer) | 9.0.18 | EF Core relational provider targeting SQL Server / LocalDB / SQL Express. |
| [`Microsoft.EntityFrameworkCore.Design`](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design) | 9.0.18 | Design-time services required by the `dotnet ef` CLI (migrations, scaffolding). Build-only, not shipped at runtime. |
| [`Swashbuckle.AspNetCore`](https://www.nuget.org/packages/Swashbuckle.AspNetCore) | 9.0.6 | Generates the OpenAPI document and Swagger UI, including the Bearer security scheme. |
| [`dotnet-ef`](https://www.nuget.org/packages/dotnet-ef) (global .NET tool, not a package reference) | 9.x | Command-line driver for EF Core migrations (`dotnet ef migrations add`, `dotnet ef database update`, ...). |
| ASP.NET Core Identity's `PasswordHasher<TUser>` | ships with the shared framework | PBKDF2-based password hashing/verification — no extra package needed, just `Microsoft.Extensions.Identity.Core` types available from `Microsoft.AspNetCore.App`. |
| `System.IdentityModel.Tokens.Jwt` / `Microsoft.IdentityModel.Tokens` | transitive (via `JwtBearer`) | Building, signing and serializing the JWT (`JwtSecurityToken`, `SymmetricSecurityKey`, `SigningCredentials`). |

No frontend/client packages are included — this is an API-only project, tested via Swagger UI, the `.http` file, or `curl`.

## Project structure

```text
JwtSecurityApi/
├── Constants/                 # Role & policy name constants (no magic strings)
│   ├── Policies.cs
│   └── Roles.cs
├── Controllers/
│   ├── AuthController.cs      # register / login / me
│   ├── ProductsController.cs  # CRUD, read = any authenticated user, write = Admin
│   └── AdminController.cs     # user listing, Admin-only policy
├── Data/
│   └── ApplicationDbContext.cs
├── Dtos/
│   ├── Auth/                  # LoginRequest, RegisterRequest, AuthResponse, UserResponse
│   └── Products/              # CreateProductRequest, UpdateProductRequest, ProductResponse
├── Migrations/                # EF Core migrations (versioned, must be committed)
├── Models/
│   ├── AppUser.cs
│   └── Product.cs
├── Options/
│   └── JwtOptions.cs          # strongly-typed, validated Jwt:* configuration section
├── Services/
│   ├── IJwtTokenService.cs / JwtTokenService.cs
│   └── DbSeeder.cs            # creates the initial Admin account from SeedAdmin:* secrets
├── Program.cs
├── appsettings.json / appsettings.Development.json
└── JwtSecurityApi.http        # ready-to-run request collection (VS / VS Code REST client)
```

## Prerequisites

- **.NET SDK 9** (or newer; verified here with SDK `10.0.300` building the `net9.0` target).
- **SQL Server** — LocalDB, SQL Server Express, or a full instance (Developer/Standard). This machine used a local `SQLEXPRESS` instance.
- **`dotnet-ef` CLI tool**: `dotnet tool install --global dotnet-ef`
- Git, and optionally the GitHub CLI (`gh`).

```powershell
dotnet --version
dotnet ef --version
git --version
```

## Quick start (run the existing code)

```powershell
git clone https://github.com/khalillakhdhar/JwtSecurityApi-DotNet9.git
cd JwtSecurityApi-DotNet9

# 1) Point EF/JWT config at your machine via User Secrets (never commit real secrets)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=JwtSecurityDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "<a random string of at least 32 bytes — see §8>"
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com"
dotnet user-secrets set "SeedAdmin:Password" "Admin123!ChangeMe"

# 2) Apply the migration that ships with the repo
dotnet ef database update

# 3) Run
dotnet run
```

Then open `https://localhost:7117/swagger` (or whatever port `dotnet run` prints).

> `appsettings.Development.json` already ships with a working (but clearly-marked, non-secret-grade) dev connection string, JWT key and seed admin, so on a machine with a local `MYPC\SQLEXPRESS`-style instance you can often skip step 1 entirely and go straight to `dotnet ef database update && dotnet run`. User Secrets are what you use once those defaults don't match your machine, or before pushing anything remotely production-like.

---

## Install dependencies — all PowerShell commands

Everything needed to go from an empty machine to a buildable project, in one place. Run from the folder containing `JwtSecurityApi.csproj` (skip the `dotnet new` line if you're cloning this repo rather than starting from scratch).

```powershell
# --- 0. Verify tooling ---
dotnet --version
dotnet --list-sdks
git --version

# --- 1. Scaffold the project (skip if you already cloned the repo) ---
dotnet new webapi -n JwtSecurityApi --use-controllers -f net9.0
cd JwtSecurityApi

# --- 2. NuGet packages ---
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.18
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.18
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.18
dotnet add package Swashbuckle.AspNetCore --version 9.0.6

# --- 3. EF Core CLI tool (global .NET tool, not a NuGet package reference) ---
dotnet tool install --global dotnet-ef
# already installed? -> dotnet tool update --global dotnet-ef
dotnet ef --version

# --- 4. Restore & build ---
dotnet restore
dotnet build

# --- 5. Per-machine secrets (User Secrets, never committed) ---
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=JwtSecurityDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$key = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:Key" $key

dotnet user-secrets set "SeedAdmin:Email" "admin@example.com"
dotnet user-secrets set "SeedAdmin:Password" "Admin123!ChangeMe"
dotnet user-secrets list

# --- 6. Database schema ---
dotnet ef migrations add InitialCreate   # only if Migrations/ doesn't exist yet
dotnet ef database update

# --- 7. Run ---
dotnet run
```

Notes:
- Step 2 versions must stay in sync with `<TargetFramework>net9.0</TargetFramework>` — don't mix a `9.0.x` framework with `8.x`/`10.x` package versions.
- If `[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)` errors in Windows PowerShell 5.1 (known static-method quirk on some setups), use `[System.Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($bytes)` instead, or generate the key from Git Bash with `head -c 64 /dev/urandom | base64 -w 0`.
- `dotnet ef migrations add InitialCreate` needs a resolvable (not necessarily reachable) connection string and a valid `Jwt:Key` already set — run step 5 before step 6.

## Step-by-step: recreate the project

A short checklist version of the full walkthrough below — follow in order, each item links to the detailed section with the actual file contents.

1. **[Create the project](#1-create-the-project)** — `dotnet new webapi -n JwtSecurityApi --use-controllers -f net9.0`.
2. **[Install NuGet packages & the EF Core tool](#2-install-nuget-packages-and-the-ef-core-tool)** — JwtBearer, EF Core SqlServer + Design, Swashbuckle, `dotnet-ef`.
3. **[Add constants](#3-project-folders--constants)** — `Roles`, `Policies` so role/policy names aren't magic strings.
4. **[Add domain models](#4-domain-models)** — `AppUser`, `Product`.
5. **[Add the DbContext](#5-dbcontext)** — `ApplicationDbContext` with the unique email index and decimal precision.
6. **[Add DTOs](#6-dtos)** — request/response contracts for auth and products; note `RegisterRequest` has no `Role`.
7. **[Add JWT options & token service](#7-jwt-options--token-service)** — `JwtOptions`, `IJwtTokenService`/`JwtTokenService`.
8. **[Configure secrets](#8-configuration--secrets)** — empty placeholders in `appsettings.json`, real values via `dotnet user-secrets`.
9. **[Wire up `Program.cs`](#9-programcs--wiring-everything-together)** — DbContext, validated `JwtOptions`, JWT Bearer authentication, `AdminOnly` policy, Swagger with a Bearer scheme.
10. **[Add controllers](#10-controllers)** — `AuthController` (register/login/me), `ProductsController` (read = any user, write = Admin), `AdminController` (Admin-only).
11. **[Add the DB seeder](#11-dbseeder--bootstrap-the-first-admin)** — `DbSeeder` creates the first `Admin` from `SeedAdmin:*` secrets.
12. **[Create & apply the EF Core migration](#12-create-and-apply-the-ef-core-migration)** — `dotnet ef migrations add InitialCreate && dotnet ef database update`.
13. **[Run and test](#13-run-and-test)** — `dotnet run`, exercise `/swagger`, confirm the 401/403/200/201 matrix.

---

## Tutorial: recreate this project from scratch

This section rebuilds the whole API file by file. Follow it top to bottom in an empty folder and you'll end up with (functionally) this repository.

### 1. Create the project

```powershell
mkdir JwtSecurityCourse
cd JwtSecurityCourse

dotnet new webapi -n JwtSecurityApi --use-controllers -f net9.0
cd JwtSecurityApi
```

Delete the generated `WeatherForecast.cs` and its controller if the template created them — this project doesn't use minimal-API sample endpoints.

### 2. Install NuGet packages and the EF Core tool

```powershell
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.18
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.18
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.18
dotnet add package Swashbuckle.AspNetCore --version 9.0.6

dotnet tool install --global dotnet-ef
# already installed? -> dotnet tool update --global dotnet-ef

dotnet restore
dotnet build
```

Your `.csproj` should now contain:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>false</InvariantGlobalization>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.18" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.18">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.18" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.6" />
  </ItemGroup>

</Project>
```

`GenerateDocumentationFile` + `NoWarn 1591` lets you write `///` XML doc-comments on controller actions (Swagger picks them up) without the compiler warning about the ones you skip.

### 3. Project folders & constants

Create the folders `Constants`, `Controllers`, `Data`, `Dtos/Auth`, `Dtos/Products`, `Models`, `Options`, `Services`.

**`Constants/Roles.cs`**

```csharp
namespace JwtSecurityApi.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
```

**`Constants/Policies.cs`**

```csharp
namespace JwtSecurityApi.Constants;

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
}
```

Centralizing these avoids typo-prone magic strings scattered across `[Authorize(Roles = "...")]` attributes.

### 4. Domain models

**`Models/AppUser.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using JwtSecurityApi.Constants;

namespace JwtSecurityApi.Models;

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(256)]
    public string NormalizedEmail { get; set; } = string.Empty;

    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Role { get; set; } = Roles.User;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

`NormalizedEmail` exists so lookups/uniqueness checks are case-insensitive without relying on a collation choice.

**`Models/Product.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace JwtSecurityApi.Models;

public sealed class Product
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
}
```

### 5. DbContext

**`Data/ApplicationDbContext.cs`**

```csharp
using JwtSecurityApi.Models;
using Microsoft.EntityFrameworkCore;

namespace JwtSecurityApi.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.Property(user => user.FullName).IsRequired();
            entity.Property(user => user.Email).IsRequired();
            entity.Property(user => user.NormalizedEmail).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.Property(user => user.Role).IsRequired();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2);

            entity.HasOne(product => product.CreatedByUser)
                .WithMany()
                .HasForeignKey(product => product.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

The unique index on `NormalizedEmail` is the real integrity guarantee (a concurrent double-registration race can't slip past a purely applicative check). `HasPrecision(18, 2)` avoids SQL Server silently picking an unsuitable `decimal` precision.

### 6. DTOs

Never expose or bind directly to your EF entities — DTOs are the contract boundary and, critically, the place where you decide what the *client* is allowed to send.

**`Dtos/Auth/RegisterRequest.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace JwtSecurityApi.Dtos.Auth;

public sealed class RegisterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$",
        ErrorMessage = "Password must contain a lowercase letter, an uppercase letter and a digit.")]
    public string Password { get; set; } = string.Empty;
}
```

Notice there is **no `Role` property** — that's deliberate (see [§10](#10-controllers)).

**`Dtos/Auth/LoginRequest.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace JwtSecurityApi.Dtos.Auth;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
```

**`Dtos/Auth/UserResponse.cs`**

```csharp
using JwtSecurityApi.Models;

namespace JwtSecurityApi.Dtos.Auth;

public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    DateTime CreatedAtUtc)
{
    public static UserResponse FromEntity(AppUser user) =>
        new(user.Id, user.FullName, user.Email, user.Role, user.CreatedAtUtc);
}
```

**`Dtos/Auth/AuthResponse.cs`**

```csharp
namespace JwtSecurityApi.Dtos.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserResponse User);
```

**`Dtos/Products/CreateProductRequest.cs`** and **`UpdateProductRequest.cs`** (identical shape):

```csharp
using System.ComponentModel.DataAnnotations;

namespace JwtSecurityApi.Dtos.Products;

public sealed class CreateProductRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 999999999)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }
}
```

**`Dtos/Products/ProductResponse.cs`**

```csharp
using JwtSecurityApi.Models;

namespace JwtSecurityApi.Dtos.Products;

public sealed record ProductResponse(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    DateTime CreatedAtUtc,
    Guid CreatedByUserId)
{
    public static ProductResponse FromEntity(Product product) =>
        new(product.Id, product.Name, product.Price, product.Stock,
            product.CreatedAtUtc, product.CreatedByUserId);
}
```

### 7. JWT options & token service

**`Options/JwtOptions.cs`**

```csharp
namespace JwtSecurityApi.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
```

**`Services/IJwtTokenService.cs`**

```csharp
using JwtSecurityApi.Models;

namespace JwtSecurityApi.Services;

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(AppUser user);
}

public sealed record JwtTokenResult(string AccessToken, DateTime ExpiresAtUtc);
```

**`Services/JwtTokenService.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JwtSecurityApi.Models;
using JwtSecurityApi.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JwtSecurityApi.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions)
    : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public JwtTokenResult CreateToken(AppUser user)
    {
        var issuedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(_jwtOptions.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.FullName),
            new("role", user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new JwtTokenResult(serializedToken, expiresAtUtc);
    }
}
```

### 8. Configuration & secrets

**`appsettings.json`** (safe to commit — no real secret values):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Issuer": "JwtSecurityApi",
    "Audience": "JwtSecurityApi.Client",
    "Key": "",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Set the real values with **User Secrets** (per-machine, outside the repo):

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=JwtSecurityDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Generate a strong random JWT signing key (must be ≥ 32 bytes for HMAC-SHA256 — the project validates this on startup and refuses to boot otherwise):

```powershell
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$key = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:Key" $key
```

> If `RandomNumberGenerator]::Fill` errors out in a Windows PowerShell 5.1 console (a known static-method-resolution quirk on some setups), generate the key with `[System.Security.Cryptography.RNGCryptoServiceProvider]` instead, or from Git Bash: `head -c 64 /dev/urandom | base64 -w 0`.

```powershell
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com"
dotnet user-secrets set "SeedAdmin:Password" "Admin123!ChangeMe"
dotnet user-secrets list
```

`dotnet user-secrets init` stamps a `<UserSecretsId>` GUID into the `.csproj` — that's what ties the project to its per-user secrets store on disk (outside the repo, never committed).

### 9. Program.cs — wiring everything together

```csharp
using System.Text;
using System.Reflection;
using JwtSecurityApi.Constants;
using JwtSecurityApi.Data;
using JwtSecurityApi.Models;
using JwtSecurityApi.Options;
using JwtSecurityApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "The 'DefaultConnection' connection string is missing. Configure it with dotnet user-secrets.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Strongly-typed, validated Jwt:* options — the app refuses to start with a weak/missing key.
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
    .Validate(o => Encoding.UTF8.GetByteCount(o.Key) >= 32, "Jwt:Key must be at least 32 bytes for HMAC-SHA256.")
    .Validate(o => o.ExpirationMinutes is > 0 and <= 1440, "Jwt:ExpirationMinutes must be between 1 and 1440.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("The Jwt configuration section is missing.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep claim names exactly as issued in the JWT

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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, policy =>
        policy.RequireAuthenticatedUser().RequireRole(Roles.Admin));
});

builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<DbSeeder>();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "JWT Security API",
        Version = "v1",
        Description = "ASP.NET Core 9 sample: JWT, roles, EF Core and SQL Server."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste only the JWT — Swagger prefixes it with 'Bearer' automatically.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "JWT Security API v1");
        options.DisplayRequestDuration();
    });

    // Learning-project convenience only: apply pending migrations (or EnsureCreated as a
    // fallback if none exist yet) and seed the admin account, at Development startup.
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var migrations = dbContext.Database.GetMigrations();
    if (migrations.Any())
        await dbContext.Database.MigrateAsync();
    else
        await dbContext.Database.EnsureCreatedAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();
}

app.UseHttpsRedirection();

// Order matters: authenticate first (build the identity), then authorize (check it).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

### 10. Controllers

**`Controllers/AuthController.cs`** — `register`, `login`, `me`. The key security decision: `RegisterRequest` has no `Role` field, so the controller always assigns `Roles.User` server-side — a client can never elevate itself to `Admin` by adding `"role":"Admin"` to the JSON body.

```csharp
var user = new AppUser
{
    FullName = request.FullName.Trim(),
    Email = email,
    NormalizedEmail = normalizedEmail,
    Role = Roles.User // never trust a role coming from the client
};
```

Login always returns the same generic "Invalid email or password" message on failure (not "user not found" vs "wrong password"), which avoids leaking whether an email is registered. It also opportunistically rehashes the password when `PasswordVerificationResult.SuccessRehashNeeded` is returned (lets you tighten hashing parameters later without forcing a mass password reset).

**`Controllers/ProductsController.cs`** — the whole controller requires `[Authorize]` (any authenticated user can `GET`); mutating actions add `[Authorize(Roles = Roles.Admin)]`.

**`Controllers/AdminController.cs`** — class-level `[Authorize(Policy = Policies.AdminOnly)]`, lists users.

See the full source of each file in [`Controllers/`](Controllers) — they're reproduced verbatim above/below the DTOs they consume, so once you've typed sections 4–9 the controllers are mostly plumbing: query via `ApplicationDbContext`, map to DTOs, return the right status code.

### 11. DbSeeder — bootstrap the first admin

**`Services/DbSeeder.cs`**

```csharp
using JwtSecurityApi.Constants;
using JwtSecurityApi.Data;
using JwtSecurityApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JwtSecurityApi.Services;

public sealed class DbSeeder(
    ApplicationDbContext dbContext,
    IPasswordHasher<AppUser> passwordHasher,
    IConfiguration configuration,
    ILogger<DbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Admin not created: SeedAdmin:Email or SeedAdmin:Password missing.");
            return;
        }

        var normalizedEmail = email.ToUpperInvariant();
        if (await dbContext.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken))
            return;

        var admin = new AppUser
        {
            FullName = "Administrator",
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = Roles.Admin
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, password);

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Initial admin created for {Email}.", email);
    }
}
```

Runs once at Development startup (see `Program.cs` §9); it's idempotent — it no-ops once an account with that normalized email exists.

### 12. Create and apply the EF Core migration

Once the model (`AppUser`, `Product`, `ApplicationDbContext`) and configuration/secrets are in place:

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

This produces a `Migrations/` folder (`InitialCreate.cs`, `InitialCreate.Designer.cs`, `ApplicationDbContextModelSnapshot.cs`) that must be **committed to source control** — it's the versioned history of your schema, not a build artifact.

Useful follow-up commands:

```powershell
dotnet ef migrations list
dotnet ef migrations remove                       # undo the last, not-yet-applied migration
dotnet ef migrations script --idempotent -o migration.sql
dotnet ef database update <PreviousMigrationName>  # roll back to a specific migration
dotnet ef database update 0                        # roll back everything
```

> **If you're retrofitting migrations onto a database that was already created by `EnsureCreatedAsync`** (as happens the first few times you run this project before any migration exists), `dotnet ef database update` will fail with *"There is already an object named 'Users' in the database"* — the tables exist but EF's `__EFMigrationsHistory` bookkeeping table doesn't know about them yet. Don't drop the database to fix this. Instead, baseline it: manually insert a row recording that the migration is already applied, matching the migration's id and EF product version:
>
> ```sql
> INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
> VALUES (N'<YourMigrationId>', N'9.0.18');
> ```
>
> From then on `dotnet ef database update` treats the schema as already in sync and any *new* migrations apply normally on top.

### 13. Run and test

```powershell
dotnet run
```

Open `/swagger`, run `POST /api/auth/login` (or `register`), copy `accessToken`, click **Authorize**, paste the token, then exercise the protected routes. See [Testing the endpoints](#testing-the-endpoints) for the `curl`/`.http` equivalents and the expected status codes for each role.

---

## API reference

| Method | Route | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Anonymous | Create a `User` account, returns a JWT. |
| `POST` | `/api/auth/login` | Anonymous | Authenticate, returns a JWT. |
| `GET`  | `/api/auth/me` | Any authenticated user | Current user's profile, reloaded from the DB. |
| `GET`  | `/api/products` | Any authenticated user | List all products. |
| `GET`  | `/api/products/{id}` | Any authenticated user | Get one product. |
| `POST` | `/api/products` | `Admin` | Create a product. |
| `PUT`  | `/api/products/{id}` | `Admin` | Update a product. |
| `DELETE` | `/api/products/{id}` | `Admin` | Delete a product. |
| `GET`  | `/api/admin/users` | `Admin` (`AdminOnly` policy) | List all users. |

`401 Unauthorized` = missing/invalid/expired token. `403 Forbidden` = valid token, wrong role.

## Configuration keys

| Key | Where | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | User Secrets / env / `appsettings.*.json` | SQL Server connection string. |
| `Jwt:Issuer` | config | Must match `ValidIssuer` at validation time. |
| `Jwt:Audience` | config | Must match `ValidAudience` at validation time. |
| `Jwt:Key` | **User Secrets only** | HMAC-SHA256 signing key, ≥ 32 bytes; startup fails fast if shorter. |
| `Jwt:ExpirationMinutes` | config | Access token lifetime, 1–1440. |
| `SeedAdmin:Email` / `SeedAdmin:Password` | User Secrets | Optional — if both are set and no matching user exists, `DbSeeder` creates the first `Admin` on Development startup. |

## Testing the endpoints

Using the bundled [`JwtSecurityApi.http`](JwtSecurityApi.http) file (VS / VS Code REST Client extension), or `curl`:

```powershell
curl -s -X POST https://localhost:7117/api/auth/register `
  -H "Content-Type: application/json" `
  -d '{"fullName":"Jane Doe","email":"jane@example.com","password":"Passw0rd!"}'

curl -s -X POST https://localhost:7117/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{"email":"jane@example.com","password":"Passw0rd!"}'

curl -s https://localhost:7117/api/products -H "Authorization: Bearer <token>"
```

Expected results, verified end-to-end against a real SQL Server instance while writing this documentation:

| Scenario | Status |
|---|---|
| Register / Login (valid credentials) | `200`/`201` with `accessToken` |
| `GET /api/products` with a `User` token | `200` |
| `POST /api/products` with a `User` token | `403` |
| `POST /api/products` with an `Admin` token | `201` |
| `GET /api/admin/users` with an `Admin` token | `200` |
| `GET /api/products` with no token | `401` |
| Tampered token (payload edited, signature no longer matches) | `401` |

## Troubleshooting

- **401 on what looks like a valid token** — check the `Bearer ` prefix, expiration, that `Jwt:Key`/`Issuer`/`Audience` match between issuing and validating (they're read from the same config, so this usually means the key changed between token issuance and now), and server clock skew.
- **403 for an Admin account** — confirm the `role` claim is present and exactly `"Admin"` (case-sensitive), and that the token was issued *after* any role change (existing tokens keep the role they were issued with until they expire).
- **SQL Server connection errors** — verify `dotnet user-secrets list`, the instance name (`.\SQLEXPRESS`, `(localdb)\MSSQLLocalDB`, ...), and that the SQL Server / SQL Browser service is running.
- **"There is already an object named 'Users'..." on `dotnet ef database update`** — see the migration-baselining note in [§12](#12-create-and-apply-the-ef-core-migration).
- **Swagger has no "Authorize" button** — check `AddSecurityDefinition`/`AddSecurityRequirement` in `Program.cs`, and that you're running in the `Development` environment (Swagger is intentionally disabled elsewhere).

## What's intentionally out of scope

This is a teaching project. For real production use, add as needed:

- Refresh tokens with rotation and revocation
- Email confirmation / password reset flows
- Account lockout after repeated failed logins, and MFA
- Audit logging, rate limiting on `/api/auth/*`
- Centralized error handling and observability (structured logging, tracing)
- Signing-key rotation / a proper secrets manager in production (User Secrets is dev-only)
- Automated unit/integration tests (e.g. `WebApplicationFactory` covering the 401/403/200/201 matrix above)

See [`SECURITY_CHECKLIST.md`](SECURITY_CHECKLIST.md) for the full pre-deployment checklist and [`COURSE_COMPLET.md`](COURSE_COMPLET.md) §30–32 for a longer discussion and suggested exercises.
