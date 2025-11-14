# 🔌 Guide Rapide - Connexion Multi-Environnements

## 🎯 Résumé en 30 secondes

Vous avez maintenant **3 environnements** configurés :

| Environnement | Base de données | Quand l'utiliser |
|--------------|-----------------|------------------|
| 🏠 **Home** | localhost | Travail à la maison |
| 🏫 **School** | DICJWIN01.cegepjonquiere.ca | Travail à l'école |
| 🔧 **Development** | localhost | Mode développement par défaut |

---

## ⚡ Méthode la plus RAPIDE

### Windows - Double-cliquez sur les fichiers :

```
📁 sallesense/
  ├── 🏫 run-school.bat    ← À l'école
  ├── 🏠 run-home.bat      ← À la maison
  └── 🔧 run-dev.bat       ← Développement
```

### Visual Studio / VS Code :

Dans le menu déroulant de lancement, choisissez :
- **SallseSense (School)** 🏫
- **SallseSense (Home)** 🏠
- **SallseSense (Development)** 🔧

---

## 📝 Méthode ligne de commande

### À l'école 🏫
```bash
cd sallesense
set ASPNETCORE_ENVIRONMENT=School
dotnet run
```

### À la maison 🏠
```bash
cd sallesense
set ASPNETCORE_ENVIRONMENT=Home
dotnet run
```

### Mode développement 🔧
```bash
cd sallesense
set ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

---

## 🔍 Comment ça fonctionne ?

ASP.NET Core charge automatiquement le bon fichier de configuration selon l'environnement :

```
ASPNETCORE_ENVIRONMENT=School
  ↓
  ↓ Charge automatiquement
  ↓
appsettings.School.json
  ↓
Connection String: DICJWIN01.cegepjonquiere.ca
```

---

## 🛠️ Modifier les configurations

### 🏫 École (appsettings.School.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DICJWIN01.cegepjonquiere.ca;Database=Prog3A25_bdSalleSense;User Id=prog3e09;Password=colonne42;TrustServerCertificate=true;"
  }
}
```

### 🏠 Maison (appsettings.Home.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Prog3A25_bdSalleSense;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

**Note :** Si vous avez un mot de passe sur votre SQL Server local, utilisez :
```json
"DefaultConnection": "Server=localhost;Database=Prog3A25_bdSalleSense;User Id=sa;Password=VotreMotDePasse;TrustServerCertificate=true;"
```

---

## ✅ Vérifier l'environnement actif

Vous pouvez ajouter ceci dans n'importe quelle page Blazor pour voir quel environnement est actif :

```razor
@inject IWebHostEnvironment Env

<div class="alert alert-info">
    Environnement actuel : <strong>@Env.EnvironmentName</strong>
</div>
```

---

## 🔒 Sécurité

**IMPORTANT** : Les fichiers de configuration avec mots de passe sont dans le `.gitignore` :

```
✅ appsettings.School.json  → Ignoré par Git
✅ appsettings.Home.json    → Ignoré par Git
❌ Ne jamais commiter de mots de passe !
```

Si quelqu'un clone votre projet, il devra créer ses propres fichiers de configuration.

---

## 🆘 Problèmes courants

### ❌ "Login failed for user"
→ Vérifiez le User Id et Password dans le fichier appsettings

### ❌ "A network-related error occurred"
→ Le serveur est inaccessible. À l'école ? Utilisez School. À la maison ? Utilisez Home.

### ❌ "Cannot open database"
→ Vérifiez que la base de données `Prog3A25_bdSalleSense` existe sur le serveur

### ❌ L'environnement ne change pas
→ Fermez complètement l'application et relancez avec le bon script

---

## 💡 Astuces

### Astuce 1 : Épingler les raccourcis
Créez des raccourcis sur votre bureau :
- École → `run-school.bat`
- Maison → `run-home.bat`

### Astuce 2 : Alias PowerShell
Ajoutez à votre profil PowerShell :
```powershell
function school { cd C:\...\sallesense; $env:ASPNETCORE_ENVIRONMENT="School"; dotnet run }
function home { cd C:\...\sallesense; $env:ASPNETCORE_ENVIRONMENT="Home"; dotnet run }
```

### Astuce 3 : Vérification rapide de connexion
```bash
# Tester si le serveur est accessible
ping DICJWIN01.cegepjonquiere.ca

# Tester la connexion SQL (si sqlcmd installé)
sqlcmd -S DICJWIN01.cegepjonquiere.ca -U prog3e09 -P colonne42 -Q "SELECT @@VERSION"
```

---

## 📚 Documentation complète

Pour plus de détails, voir [README_CONNEXION.md](README_CONNEXION.md)
