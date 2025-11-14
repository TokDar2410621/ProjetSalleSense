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
├── interface_connexion.py                # Login GUI
├── interface_principale.py               # Main monitoring dashboard
├── lancer_interface.py                   # GUI launcher
├── capture_photos_continu.py             # Camera monitoring (Raspberry Pi)
├── capture_son_continu.py                # Audio monitoring (Raspberry Pi)
├── surveillance_intelligente.py          # Intelligent monitoring system
└── db_config.json                        # Saved connection settings (no passwords)
```

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

The web application uses scoped services registered in [Startup.cs](sallesense/Startup.cs):

- **AuthService**: User registration and login (calls stored procedures)
- **PhotoService**: Retrieves sensor photo/video data
- **ReservationService**: Reservation CRUD operations
- **CustomAuthenticationStateProvider**: Blazor authentication state with ProtectedSessionStorage

All services use `IDbContextFactory<Prog3A25BdSalleSenseContext>` for database access (supports concurrent operations in Blazor Server).

### Python IoT Architecture

Python scripts connect directly to SQL Server using pyodbc:

- **DatabaseConnection** class wraps pyodbc connection with convenience methods
- **Tkinter GUI** provides real-time monitoring dashboard
- **Raspberry Pi scripts** run with sudo for GPIO access
- GPIO pins: 17 (green LED), 4 (red LED), 27 (button/input)
- **db_config.json** stores last connection settings (excluding passwords for security)

## Development Workflow

### Adding a New Blazor Page

1. Create `.razor` file in `sallesense/Pages/`
2. Add `@page "/route"` directive at the top
3. Inject required services: `@inject AuthService AuthService`
4. Add navigation link in `sallesense/Shared/NavMenu.razor`
5. Use `NavigationManager.NavigateTo()` for programmatic navigation

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

## Important Notes

- **Never manually edit scaffolded models** in `sallesense/Models/` - they will be overwritten
- **Blazor Server uses SignalR** - be aware of connection lifetime and state management
- **Python scripts require Raspberry Pi hardware** for GPIO operations (picamera2, RPi.GPIO)
- **Stored procedures must return values** - use OUTPUT parameters or RETURN statements
- **SQL parameter names are case-sensitive** in stored procedures
- **French naming in database is intentional** - do not anglicize column names
- **Triggers enforce data integrity** - test reservation overlaps and blacklist scenarios
- **Passwords are never stored in config files** in Python scripts (security best practice)

## Documentation

- [README.md](README.md) - Project overview (already exists, superseded by this file for development)
- [DARIUS.md](DARIUS.md) - Line-by-line AuthService code explanation (learning resource)
- [pythonRAs/USAGE_INTERFACE.md](pythonRAs/USAGE_INTERFACE.md) - Python GUI usage guide
- [pythonRAs/USAGE_CAMERAS.md](pythonRAs/USAGE_CAMERAS.md) - Camera sensor documentation
- [pythonRAs/USAGE_MICRO.md](pythonRAs/USAGE_MICRO.md) - Audio sensor documentation
- [pythonRAs/USAGE_SURVEILLANCE.md](pythonRAs/USAGE_SURVEILLANCE.md) - Intelligent monitoring system
- [Dcumentation/Demarrage.pdf](Dcumentation/Demarrage.pdf) - User flow mockups
- [Modelisation.png](Modelisation.png) - Database ER diagram
