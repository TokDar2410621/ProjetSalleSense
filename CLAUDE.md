# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SalleSense is a room management system with IoT sensor integration. The project consists of two main components:

1. **Blazor Server Web Application** (C# .NET 9.0) - Room reservation and management interface
2. **Python Hardware Scripts** (Raspberry Pi) - IoT sensor monitoring and data collection

The architecture follows a database-first approach where SQL Server stored procedures enforce business logic, and both the web application and Python scripts interact with the same SQL Server database.

## Technology Stack

### Web Application (sallesense/)
- **.NET 9.0** with Blazor Server (server-side rendering with SignalR)
- **Entity Framework Core 9.0.10** - Database access with DbContextFactory pattern
- **SQL Server** - Central database with stored procedures, triggers, and views
- **User Secrets** (ID: bc0528ed-92c2-4722-967c-aeb0aa431703)

### IoT/Hardware (pythonRAs/)
- **Python 3** with Raspberry Pi GPIO
- **pyodbc** - SQL Server connectivity
- **picamera2** - Camera module control
- **Tkinter** - Desktop GUI for monitoring

## Common Commands

### Web Application

```bash
# Navigate to web app directory
cd sallesense

# Run the application (starts on https://localhost:5001)
dotnet run

# Run with specific environment (Home, School, or Development)
# Windows PowerShell:
$env:ASPNETCORE_ENVIRONMENT="Home"; dotnet run
# Or use convenience scripts:
./run-home.bat     # Uses LocalDB
./run-school.bat   # Uses school SQL Server
./run-dev.bat      # Development environment

# Build the project
dotnet build

# Restore dependencies
dotnet restore

# Regenerate EF Core models after database schema changes
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context-dir Data --context Prog3A25BdSalleSenseContext --force
```

### Python IoT Scripts

```bash
# Navigate to Python directory
cd pythonRAs

# Install Python dependencies
pip install -r requirements.txt

# Launch the GUI interface (requires database connection)
python lancer_interface.py

# Run sensor monitoring scripts (requires Raspberry Pi hardware)
sudo python capture_photos_continu.py
sudo python capture_son_continu.py
sudo python surveillance_intelligente.py
```

### Database Setup

Execute SQL scripts in this order:
```bash
# 1. Create database
sqlcmd -S server_name -U username -P password -i Script_bd/ceration_bd.sql

# 2. Create tables
sqlcmd -S server_name -U username -P password -i Script_bd/creationTables.sql

# 3. Add constraints
sqlcmd -S server_name -U username -P password -i Script_bd/contrainteSql.sql

# 4. Create triggers
sqlcmd -S server_name -U username -P password -i Script_bd/Trigger.sql

# 5. Create stored procedures
sqlcmd -S server_name -U username -P password -i Script_bd/ProcedureStocke.sql
sqlcmd -S server_name -U username -P password -i Script_bd/ProceduresAvancees.sql

# 6. Create views
sqlcmd -S server_name -U username -P password -i Script_bd/View.sql
sqlcmd -S server_name -U username -P password -i Script_bd/ViewsComplexes.sql

# 7. Insert sample data
sqlcmd -S server_name -U username -P password -i Script_bd/InsertionSql.sql
```

## Architecture

### Database-First Architecture

The database (`Prog3A25_bdSalleSense`) is the source of truth. Key characteristics:

- **Stored Procedures** handle business logic (authentication, user creation, data validation)
- **Triggers** enforce constraints (no reservation overlaps, blacklist enforcement)
- **Views** provide data aggregation
- **French naming convention** in database (e.g., `idUtilisateur_PK`, `capaciteMaximale`)

**Critical**: When modifying the database schema:
1. Update SQL scripts in `Script_bd/`
2. Execute scripts against the database
3. Re-scaffold EF Core models using the `dotnet ef dbcontext scaffold` command
4. Never manually edit generated model classes in `sallesense/Models/`

### Web Application Structure

```
sallesense/
├── Data/
│   └── Prog3A25BdSalleSenseContext.cs    # EF Core DbContext (scaffolded)
├── Models/                                # EF Core entities (scaffolded from DB)
├── Services/
│   ├── Authservice.cs                     # Authentication (calls stored procedures)
│   ├── PhotoService.cs                    # Photo data retrieval
│   └── ReservationService.cs              # Reservation business logic
├── Authentication/
│   └── CustomAuthenticationStateProvider.cs  # Blazor authentication
├── Pages/                                 # Blazor pages (.razor files)
│   ├── Index.razor                       # Landing page
│   ├── Login.razor / Register.razor      # Authentication pages
│   ├── Dashboard.razor                   # Main dashboard
│   ├── Salles.razor                      # Room list with filters
│   ├── SalleDetails.razor                # Room details
│   ├── CreerReservation.razor            # Create reservation
│   └── ModifierReservation.razor         # Edit reservation
├── Shared/                               # Shared Blazor components
├── Program.cs                            # Entry point
└── Startup.cs                            # Service configuration
```

### Python Scripts Structure

```
pythonRAs/
├── db_connection.py                      # Database wrapper with auth methods
├── config.py                             # Configuration management
├── db_config.json                        # Saved connection settings (no passwords)
│
├── PRODUCTION MONITORING (Raspberry Pi):
├── lancer_interface.py                   # GUI launcher (entry point)
├── interface_connexion.py                # Login GUI
├── interface_principale.py               # Main monitoring dashboard (44KB - complex)
├── capture_photos_continu.py             # Camera monitoring with LED indicators
├── capture_son_continu.py                # Audio level monitoring
├── surveillance_intelligente.py          # Intelligent monitoring system
│
├── DEVELOPMENT/TESTING:
├── inserer_screenshots.py                # Bulk insert photos from filesystem
├── test_photos_bd.py                     # Photo BLOB diagnostic
├── test_envoi_donnees.py                 # Test sensor data insertion
├── test_animation_barre.py               # Audio level bar animation test
├── verif_quick.py                        # Quick connectivity check
├── visualiser_photos.py                  # Export photos from database
├── visualiser_videos.py                  # Video data viewer
├── sensor_monitor.py                     # Generic sensor monitoring
│
├── UTILITIES:
├── initialiser_bd.py                     # Database initialization
├── exemple_get_user.py                   # User retrieval example
├── boutton.py                            # GPIO button example
├── labo.py                               # Laboratory/testing script
└── proto-final.py                        # Prototype script
```

**Key Python Scripts**:
- **lancer_interface.py**: Main entry point, launches authentication then dashboard
- **interface_principale.py**: Complex 45KB Tkinter GUI with real-time sensor monitoring
- **capture_photos_continu.py**: Runs on Raspberry Pi, captures photos continuously, controls green/red LEDs
- **capture_son_continu.py**: Audio level monitoring with dB calculation
- **surveillance_intelligente.py**: Smart monitoring with anomaly detection
- **inserer_screenshots.py**: Utility to populate database with test photos (supports LocalDB + SQL Server)

### Database Schema

8 core tables with enforced relationships:

1. **Utilisateur** - Users with SHA2_256 hashed passwords (salt + hash stored separately)
2. **Blacklist** - Banned users (foreign key to Utilisateur)
3. **Salle** - Rooms with capacity (numero, capaciteMaximale)
4. **Reservation** - Bookings with time slots, linked to Utilisateur and Salle
5. **Capteur** - Sensors (types: MOUVEMENT, BRUIT, CAMERA)
6. **Donnees** - Sensor data (measurements or photo/video paths)
7. **Evenement** - Events triggered by sensor data
8. **Avertissement** - Warnings with trigger enforcement

**Primary Keys**: Suffixed with `_PK` in database (e.g., `idUtilisateur_PK`)
**Foreign Keys**: Suffixed with `_FK` in database (e.g., `idUtilisateur_FK`)

### Critical Triggers

- `trg_pasDeChevauchement` on Reservation: Prevents overlapping reservations for same room
- `verifierNombreAvertissement` on Avertissement: Enforces warning limits
- `trg_check_blacklist`: Prevents blacklisted users from making reservations

### Authentication Flow

Both web and Python apps use the same stored procedures:

**Registration** (returns new user ID or -1 if email exists):
```sql
EXEC dbo.usp_Utilisateur_Create
  @Pseudo = 'username',
  @Courriel = 'user@example.com',
  @MotDePasse = 'plaintext_password'  -- Hashed by stored procedure
```

**Login** (returns user ID or -1 if invalid, -2 if blacklisted):
```sql
EXEC dbo.usp_Utilisateur_Login
  @Courriel = 'user@example.com',
  @MotDePasse = 'plaintext_password'
```

**Implementation in C#** ([Services/Authservice.cs](sallesense/Services/Authservice.cs)):
- Uses `SqlParameter` with `@RETURN_VALUE` (exact name required by ADO.NET)
- Returns tuples: `(bool success, int userId, string message)`
- No parentheses when executing stored procedures: `EXEC @RETURN_VALUE = dbo.usp_Utilisateur_Create @Pseudo, @Courriel, @MotDePasse`

**Implementation in Python** ([db_connection.py](pythonRAs/db_connection.py)):
- Uses `pyodbc` with OUTPUT parameters
- Class methods: `create_user()`, `login_user()`, `get_user_by_id()`

### Service Layer Pattern

The web application uses scoped services registered in [Startup.cs](sallesense/Startup.cs). All services use `IDbContextFactory<Prog3A25BdSalleSenseContext>` for database access (supports concurrent operations in Blazor Server).

**Core Services** ([Services/](sallesense/Services/)):
- **AuthService**: User registration and login (calls stored procedures `usp_Utilisateur_Create`, `usp_Utilisateur_Login`)
- **AdminService**: User management, blacklist operations
- **ReservationService**: Reservation CRUD with overlap validation
- **ReservationFormService**: Reservation form logic and available rooms
- **ModifierReservationService**: Edit existing reservations
- **PhotoService**: Photo BLOB retrieval from database (see BLOB handling notes below)
- **ProfilService**: User profile management, password changes
- **DashboardService**: Statistics and dashboard data aggregation
- **SalleDetailsService**: Room details and sensor data
- **HomeService**: Home page data
- **CustomAuthenticationStateProvider**: Blazor authentication state with ProtectedSessionStorage

### Python IoT Architecture

Python scripts connect directly to SQL Server using pyodbc:

- **DatabaseConnection** class wraps pyodbc connection with convenience methods
- **Tkinter GUI** provides real-time monitoring dashboard
- **Raspberry Pi scripts** run with sudo for GPIO access
- GPIO pins: 17 (green LED), 4 (red LED), 27 (button/input)
- **db_config.json** stores last connection settings (excluding passwords for security)

## Development Workflow

### Adding a New Blazor Page (Code-Behind Pattern)

**Important**: This project uses the code-behind pattern to separate markup from logic (see MODIFICATIONS.md for rationale).

**Create the Razor view** (`Pages/MyPage.razor`):
```razor
@page "/my-route"
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize]

<!-- HTML markup only - no @code blocks -->
<h1>@Title</h1>
<button @onclick="HandleClick">Click Me</button>
```

**Create the code-behind** (`Pages/MyPage.razor.cs`):
```csharp
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SallseSense.Services;

namespace SallseSense.Pages
{
    public partial class MyPage : ComponentBase
    {
        [Inject]
        protected MyService MyService { get; set; } = default!;

        [Inject]
        protected NavigationManager Navigation { get; set; } = default!;

        protected string Title { get; set; } = "My Page";

        protected override async Task OnInitializedAsync()
        {
            // Initialization logic
        }

        protected async Task HandleClick()
        {
            // Event handler logic
        }
    }
}
```

**Add navigation** in `sallesense/Shared/NavMenu.razor`:
```razor
<div class="nav-item px-3">
    <NavLink class="nav-link" href="/my-route">
        <span class="oi oi-icon" aria-hidden="true"></span> My Page
    </NavLink>
</div>
```

**Benefits of code-behind pattern**:
- Separation of concerns (markup vs logic)
- Better testability
- Improved IntelliSense in .cs files
- Easier to maintain and navigate
- Follows ASP.NET Core MVC conventions

### Modifying Database Schema

1. Edit SQL in `Script_bd/creationTables.sql` or create new migration script
2. Run SQL script against database
3. Navigate to `sallesense/` directory
4. Run: `dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context-dir Data --context Prog3A25BdSalleSenseContext --force`
5. Review generated models for any manual adjustments needed in business logic

### Adding IoT Sensor Integration

1. Add sensor data collection logic in new Python script
2. Use `DatabaseConnection` class from `db_connection.py` to connect
3. Insert sensor data using stored procedures or direct SQL
4. Test data flow appears in Blazor web interface
5. Update `interface_principale.py` if new sensor types need dashboard display

## Key Design Patterns

### DbContextFactory Pattern
The web application uses `IDbContextFactory<Prog3A25BdSalleSenseContext>` instead of direct DbContext injection. This is critical for Blazor Server because:
- Prevents DbContext lifetime issues with long-lived SignalR connections
- Allows concurrent database operations
- Each operation creates and disposes its own DbContext

Example usage:
```csharp
await using var db = await _factory.CreateDbContextAsync();
// Use db for queries
```

### Stored Procedure Security
Authentication logic lives in stored procedures, not application code:
- Passwords are hashed with SHA2_256 + salt in the database
- Application never handles password hashing
- SQL Server enforces business rules via triggers and constraints

### Separation of Concerns
- **Database**: Business rules (triggers, constraints, stored procedures)
- **Services**: Orchestration and data transformation
- **Pages**: UI and user interaction
- **Python Scripts**: Hardware interfacing and data collection

## Naming Conventions

- **Database**: French (e.g., `idUtilisateur_PK`, `capaciteMaximale`, `heureDebut`)
- **C# Classes/Methods**: PascalCase (e.g., `AuthService`, `RegisterAsync`)
- **C# Variables**: camelCase (e.g., `userId`, `connectionString`)
- **C# Private Fields**: _camelCase (e.g., `_factory`, `_logger`)
- **Python**: snake_case (e.g., `create_user`, `db_connection`)
- **SQL Parameters**: @PascalCase (e.g., `@Pseudo`, `@Courriel`)

## Configuration

### Web Application Connection String
Stored in user secrets (never commit to version control):
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=SERVER_NAME;Database=Prog3A25_bdSalleSense;User Id=USERNAME;Password=PASSWORD;TrustServerCertificate=True;"
```

User secrets ID: `bc0528ed-92c2-4722-967c-aeb0aa431703`

### Python Database Configuration
Edit server/database/credentials at the top of Python scripts or in `db_config.json` (passwords not saved):
```python
SERVER = "DICJWIN01.cegepjonquiere.ca"
DATABASE = "Prog3A25_bdSalleSense"
USERNAME = "prog3e09"
PASSWORD = "colonne42"  # Example - use your credentials
```

## Working with Photo BLOBs

Photos are stored as `VARBINARY(MAX)` in the `Donnees` table. **Critical LINQ-to-SQL limitation**:

**❌ WRONG - Will throw InvalidOperationException**:
```csharp
var photos = await context.Donnees
    .Where(d => d.PhotoBlob != null && d.PhotoBlob.Length > 0)  // Error: .Length not translatable
    .Select(d => new PhotoInfo { Size = d.PhotoBlob.Length })
    .ToListAsync();
```

**✅ CORRECT - Load into memory first**:
```csharp
// Step 1: Retrieve from database
var donnees = await context.Donnees
    .Where(d => d.PhotoBlob != null)
    .ToListAsync();

// Step 2: Filter and project in memory
var photos = donnees
    .Where(d => d.PhotoBlob.Length > 0)
    .Select(d => new PhotoInfo {
        Id = d.IdDonneePk,
        Size = d.PhotoBlob.Length  // Now works because data is in memory
    })
    .ToList();
```

**Photo API Endpoint**: `GET /api/photo/{id}` ([Controllers/PhotoController.cs](sallesense/Controllers/PhotoController.cs))
- Auto-detects JPEG/PNG via magic bytes
- Returns appropriate Content-Type
- Used in views: `<img src="/api/photo/@photoId" />`

**Inserting photos** (Python): Use [inserer_screenshots.py](pythonRAs/inserer_screenshots.py) to bulk insert photos from filesystem.

## Testing

### Test Data Scripts

Execute these in order to set up test data ([GUIDE_TESTS.md](GUIDE_TESTS.md)):

```bash
cd Script_bd

# 1. Create 3 test rooms
sqlcmd -S (localdb)\MSSQLLocalDB -d Prog3A25_bdSalleSense -i Insert_3_Salles.sql

# 2. Add sensors and sample data
sqlcmd -S (localdb)\MSSQLLocalDB -d Prog3A25_bdSalleSense -i Insert_Capteurs_Donnees.sql

# 3. Setup admin role and test users
sqlcmd -S (localdb)\MSSQLLocalDB -d Prog3A25_bdSalleSense -i Ajout_Role_Admin.sql
```

**Test accounts created**:
- **Admin**: tokamdaruis@gmail.com (your existing password)
- **Test User**: user.test@example.com / test123

### Key Test Scenarios

1. **Reservation overlap prevention**: Create reservation, then try overlapping time slot (should fail via `trg_pasDeChevauchement`)
2. **Blacklist enforcement**: Admin blacklists user → user cannot login (`usp_Utilisateur_Login` returns -2)
3. **Capacity validation**: Reserve room with more people than capacity → validation error
4. **Photo display**: Visit `/salle-details/1` → should show camera photos in archive section
5. **Admin functions**: Only visible to users with `role = 'Admin'`

### Python Testing Scripts

- `test_photos_bd.py` - Verify photo BLOBs in database
- `test_envoi_donnees.py` - Test sensor data insertion
- `verif_quick.py` - Quick database connectivity check
- `visualiser_photos.py` - Export and view photos from database
- `visualiser_videos.py` - Video data viewer

## Important Notes

- **Never manually edit scaffolded models** in `sallesense/Models/` - they will be overwritten
- **Use code-behind pattern** for all new Blazor pages (`.razor` + `.razor.cs`)
- **BLOB queries**: Always load BLOBs into memory before accessing `.Length` property
- **Blazor Server uses SignalR** - be aware of connection lifetime and state management
- **Python scripts require Raspberry Pi hardware** for GPIO operations (picamera2, RPi.GPIO)
- **Stored procedures must return values** - use OUTPUT parameters or RETURN statements
- **SQL parameter names are case-sensitive** in stored procedures
- **French naming in database is intentional** - do not anglicize column names
- **Triggers enforce data integrity** - test reservation overlaps and blacklist scenarios
- **Passwords are never stored in config files** in Python scripts (security best practice)
- **Environment matters**: LocalDB (Home) vs SQL Server (School/Dev) have different data
- **Multiple appsettings files**: appsettings.Home.json, appsettings.School.json, appsettings.Development.json

## Documentation

### Core Documentation
- [README.md](README.md) - Project overview and quick start
- **CLAUDE.md** (this file) - Comprehensive development guide for Claude Code
- [DARIUS.md](DARIUS.md) - Line-by-line AuthService code explanation (learning resource)
- [GUIDE_TESTS.md](GUIDE_TESTS.md) - Testing scenarios and test data setup
- [MODIFICATIONS.md](MODIFICATIONS.md) - Code-behind refactoring session notes (2025-11-15)
- [SESSION_2025-11-19_PHOTOS.md](SESSION_2025-11-19_PHOTOS.md) - Photo BLOB implementation details
- [Modelisation.png](Modelisation.png) - Database ER diagram

### Python IoT Documentation
- [pythonRAs/readme.md](pythonRAs/readme.md) - Python scripts overview
- [pythonRAs/USAGE_INTERFACE.md](pythonRAs/USAGE_INTERFACE.md) - Tkinter GUI usage guide
- [pythonRAs/USAGE_CAMERAS.md](pythonRAs/USAGE_CAMERAS.md) - Camera sensor setup and usage
- [pythonRAs/USAGE_MICRO.md](pythonRAs/USAGE_MICRO.md) - Audio sensor documentation
- [pythonRAs/USAGE_SURVEILLANCE.md](pythonRAs/USAGE_SURVEILLANCE.md) - Intelligent monitoring system
- [pythonRAs/guide_capteurs.md](pythonRAs/guide_capteurs.md) - Sensor integration guide
- [pythonRAs/ANIMATION_BARRE_SON.md](pythonRAs/ANIMATION_BARRE_SON.md) - Audio bar animation implementation

### User Documentation
- [Dcumentation/Demarrage.pdf](Dcumentation/Demarrage.pdf) - User flow mockups and wireframes
- [Autorisation_roles.pdf](Autorisation_roles.pdf) - Role-based authorization documentation
- [sallesense/GUIDE_CONNEXION.md](sallesense/GUIDE_CONNEXION.md) - Connection setup guide
- [sallesense/README_CONNEXION.md](sallesense/README_CONNEXION.md) - Connection troubleshooting
