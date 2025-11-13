
This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SalleSense is a room management system built with ASP.NET Core Blazor Server and SQL Server. The application tracks room reservations, sensors, events, and user management with a blacklist feature.

## Technology Stack

- **.NET 9.0** - Target framework
- **Blazor Server** - UI framework with server-side rendering
- **Entity Framework Core 9.0.10** - ORM for database access
- **SQL Server** - Database with stored procedures, triggers, and views
- **User Secrets** - Configuration management (ID: bc0528ed-92c2-4722-967c-aeb0aa431703)

## Common Commands

### Running the Application
```bash
cd SallseSense
dotnet run
```

### Building the Project
```bash
cd SallseSense
dotnet build
```

### Restoring Dependencies
```bash
cd SallseSense
dotnet restore
```

### Database Scaffolding
When the database schema changes, regenerate the EF Core models:
```bash
cd SallseSense
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context-dir Data --context Prog3A25BdSalleSenseContext --force
```

## Architecture

### Project Structure

The application follows a layered architecture:

- **SallseSense/** - Main Blazor Server application
  - **Models/** - EF Core entity classes (scaffolded from database)
  - **Data/** - DbContext (`Prog3A25BdSalleSenseContext`) and data services
  - **Pages/** - Blazor pages/components (`.razor` files)
  - **Shared/** - Shared Blazor components (layouts, navigation)
  - **Services/** - Application services
  - **wwwroot/** - Static files (CSS, JS, images)

- **Script_bd/** - SQL Server database scripts
  - Database initialization and table creation
  - Stored procedures (basic and advanced)
  - Triggers and constraints
  - Views (simple and complex)

### Database Architecture

The database (`Prog3A25_bdSalleSense`) consists of 8 main tables:

1. **Utilisateur** - Users with authentication (pseudo, courriel, motDePasse with salt/hash)
2. **Blacklist** - Blacklisted users
3. **Salle** - Rooms with capacity (numero, capaciteMaximale)
4. **Reservation** - Room reservations with time slots and person count
5. **Capteur** - Sensors (nom, type)
6. **Donnees** - Sensor data (measurements, photos, timestamps)
7. **Evenement** - Events linked to sensor data
8. **Avertissement** - Warnings (with trigger: `verifierNombreAvertissement`)

### Key Database Features

- **Triggers:**
  - `verifierNombreAvertissement` on Avertissement table
  - `trg_pasDeChevauchement` on Reservation table (prevents overlapping reservations)

- **Stored Procedures:** Located in `Script_bd/ProcedureStocke.sql` and `Script_bd/ProceduresAvancees.sql`

- **Views:** Located in `Script_bd/View.sql` and `Script_bd/ViewsComplexes.sql`

- **Constraints:** Advanced check constraints and business rules in `Script_bd/checkConstraintsAvancees.sql` and `Script_bd/contraintesAvancees.sql`

### Application Configuration

- **Startup.cs** - Configures services and middleware:
  - DbContextFactory registration for `Prog3A25BdSalleSenseContext`
  - Connection string: "DefaultConnection" (stored in user secrets)
  - AuthService registration (`services.AddScoped<AuthService>()`)
  - Blazor Server with SignalR hub
  - MVC controllers and Razor pages support

- **Program.cs** - Application entry point using Host builder pattern with Serilog logging

- **appsettings.json** - Logging configuration (no sensitive data stored here)

### Entity Framework Context

The `Prog3A25BdSalleSenseContext` (in [Data/Prog3A25BdSalleSenseContext.cs](SallseSense/Data/Prog3A25BdSalleSenseContext.cs)) is generated via EF Core scaffolding and includes:
- DbSet properties for all 8 entities
- Trigger metadata in `OnModelCreating`
- Connection string configuration

**Important:** The Models are scaffolded from the database. When making schema changes:
1. Update SQL scripts in `Script_bd/`
2. Run the scripts against the database
3. Re-scaffold the models using the command above

## Database Setup

To initialize the database, run the SQL scripts in this order:

1. `Script_bd/ceration_bd.sql` - Creates the database
2. `Script_bd/creationTables.sql` - Creates tables
3. `Script_bd/contrainteSql.sql` or `Script_bd/contraintesAvancees.sql` - Adds constraints
4. `Script_bd/Trigger.sql` - Creates triggers
5. `Script_bd/ProcedureStocke.sql` - Creates stored procedures
6. `Script_bd/View.sql` - Creates views
7. `Script_bd/InsertionSql.sql` - Inserts sample data

## Naming Conventions

- **Database:** French naming convention (e.g., `idUtilisateur_PK`, `capaciteMaximale`, `heureDebut`)
- **C# Models:** PascalCase properties mapped to French database columns via `[Column]` attributes
- **Primary Keys:** Suffixed with `_PK` in database, mapped to `*Pk` properties in models

## Application User Flow

Based on the documentation in [Dcumentation/Demarrage.pdf](Dcumentation/Demarrage.pdf), the application follows this user flow:

1. **Démarrage (Start)** → Landing page with two options:
   - **Inscription** (Create account) → Registration form
   - **Se connecter** (Login) → Login form if user already has an account

2. **After Authentication** → Main dashboard showing:
   - **Voir salle** (View rooms) → Browse available rooms with filtering options
   - **Statistiques** (Statistics) → View activity statistics and recent reservations

3. **Room Management**:
   - **Toutes les détails** (All rooms) → List of all rooms with search functionality
   - **Détails d'une salle** (Room details) → Detailed view of a specific room showing:
     - Room number, capacity, availability
     - Photo archive
     - Recent activity history

4. **Reservation Management**:
   - **Créer une réservation** (Create reservation) → Form with:
     - Room selection
     - Date and time selection
     - Number of people
     - Warning if room capacity exceeded or unavailable
   - **Modifier une réservation** (Modify reservation) → Edit existing reservations with:
     - Change date/time
     - Update number of people
     - Option to cancel

## Blazor Pages

Implemented pages:
- `/` - Home page ([Pages/Index.razor](SallseSense/Pages/Index.razor))
- `/register` - Registration page with form validation ([Pages/Register.razor](SallseSense/Pages/Register.razor))
- `/login` - Login page with "Remember Me" option ([Pages/Login.razor](SallseSense/Pages/Login.razor))
- `/dashboard` - Main dashboard with statistics and room overview ([Pages/Dashboard.razor](SallseSense/Pages/Dashboard.razor))
- `/salles` - Paginated room list with search and filters ([Pages/Salles.razor](SallseSense/Pages/Salles.razor))
- `/salle-details/{id}` - Detailed room view with sensors and reservations ([Pages/SalleDetails.razor](SallseSense/Pages/SalleDetails.razor))
- `/creer-reservation` - Create new reservation form ([Pages/CreerReservation.razor](SallseSense/Pages/CreerReservation.razor))
- `/modifier-reservation/{id}` - Modify existing reservation ([Pages/ModifierReservation.razor](SallseSense/Pages/ModifierReservation.razor))
- `/counter` - Counter demo page (can be removed)
- `/fetchdata` - Weather data demo page (can be removed)

The application uses Blazor Server-Side rendering with SignalR for real-time communication.

## Authentication System

### AuthService ([Services/Authservice.cs](SallseSense/Services/Authservice.cs))

The authentication service handles user registration and login using stored procedures:

**RegisterAsync(pseudo, courriel, motDePasse)**
- Calls stored procedure `usp_Utilisateur_Create` (in `Script_bd/ProceduresAvancees.sql`)
- Returns tuple: `(bool success, int userId, string message)`
- Password is hashed in the stored procedure using `HASHBYTES('SHA2_256', ...)`
- Validates input (minimum 6 characters, required fields)
- Logs errors with ILogger

**LoginAsync(courriel, motDePasse)**
- Calls stored procedure `usp_Utilisateur_Login` (in `Script_bd/ProceduresAvancees.sql`)
- Returns tuple: `(bool success, int userId, string message)`
- Verifies password hash in stored procedure
- Logs authentication attempts and errors

### Authentication Implementation Notes

**Current Implementation:**
- Basic authentication using stored procedures
- Session management not yet implemented
- Pages redirect to `/login` or `/dashboard` after auth operations

**To Implement "Remember Me" Feature:**
1. Add `services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)` in Startup.cs
2. Add `services.AddHttpContextAccessor()` in Startup.cs
3. Use `HttpContext.SignInAsync()` in Login.razor with `IsPersistent = rememberMe`
4. Set cookie expiration: 14 days if rememberMe, 1 hour otherwise
5. Use `[Authorize]` attribute on protected pages

**Logging:**
- Serilog configured in Program.cs
- Logs written to `SallseSense/logs/app-YYYYMMDD.txt`
- Format: `[Timestamp] [Level] Message + Exception`

## Project Documentation

- **[Dcumentation/Demarrage.pdf](Dcumentation/Demarrage.pdf)** - Complete user flow and UI mockups
- **[Modelisation.png](Modelisation.png)** - Database entity relationship diagram
- **[DARIUS.md](DARIUS.md)** - Line-by-line explanation of AuthService code (for learning)
