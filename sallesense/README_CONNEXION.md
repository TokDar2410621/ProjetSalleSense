# Configuration des environnements de connexion

## 🎯 Fichiers de configuration

Trois fichiers de configuration sont disponibles :

1. **appsettings.Development.json** - Configuration par défaut (localhost)
2. **appsettings.School.json** - Configuration pour l'école
3. **appsettings.Home.json** - Configuration pour la maison

## 🚀 Lancement rapide

### Option 1 : Double-cliquer sur les fichiers .bat

- **run-school.bat** - Lance l'application avec la BD de l'école
- **run-home.bat** - Lance l'application avec la BD de la maison
- **run-dev.bat** - Lance l'application en mode développement (localhost)

### Option 2 : Ligne de commande

#### À l'école :
```bash
cd sallesense
set ASPNETCORE_ENVIRONMENT=School
dotnet run
```

#### À la maison :
```bash
cd sallesense
set ASPNETCORE_ENVIRONMENT=Home
dotnet run
```

#### Mode développement (localhost) :
```bash
cd sallesense
set ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

### Option 3 : Visual Studio / VS Code

Dans les propriétés de lancement, modifier la variable d'environnement :

**launchSettings.json** (créer dans Properties/) :
```json
{
  "profiles": {
    "Development": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "applicationUrl": "https://localhost:5001;http://localhost:5000"
    },
    "School": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "School"
      },
      "applicationUrl": "https://localhost:5001;http://localhost:5000"
    },
    "Home": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Home"
      },
      "applicationUrl": "https://localhost:5001;http://localhost:5000"
    }
  }
}
```

## 🔧 Modifier les configurations

### Pour l'école (appsettings.School.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DICJWIN01.cegepjonquiere.ca;Database=Prog3A25_bdSalleSense;User Id=prog3e09;Password=colonne42;TrustServerCertificate=true;"
  }
}
```

### Pour la maison (appsettings.Home.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Prog3A25_bdSalleSense;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

**OU si vous utilisez SQL Server Authentication à la maison :**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Prog3A25_bdSalleSense;User Id=sa;Password=VotreMotDePasse;TrustServerCertificate=true;"
  }
}
```

## 🔒 Sécurité

**IMPORTANT** : Les fichiers `appsettings.School.json` et `appsettings.Home.json` sont dans le `.gitignore` pour ne pas commiter les mots de passe.

Si vous clonez le projet, vous devrez créer vos propres fichiers avec vos identifiants.

## 🧪 Tester la connexion

Après avoir lancé l'application, vous devriez voir dans la console :
```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:5000
```

Allez sur https://localhost:5001 et essayez de vous connecter.

## ❌ Dépannage

### Erreur de connexion à la BD

Si vous voyez une erreur SQL, vérifiez :

1. **Le serveur est-il accessible ?**
   ```bash
   ping DICJWIN01.cegepjonquiere.ca
   ```

2. **Les identifiants sont-ils corrects ?**
   - Vérifiez le User Id et Password dans le fichier appsettings

3. **Le nom de la base de données est-il correct ?**
   - Doit être : `Prog3A25_bdSalleSense`

4. **TrustServerCertificate est-il activé ?**
   - Nécessaire pour SQL Server 2022+

### L'environnement ne change pas

Si l'application utilise toujours la même connexion :

1. Fermez complètement l'application
2. Relancez avec le bon script .bat
3. Vérifiez dans la console que `ASPNETCORE_ENVIRONMENT` est bien défini

### Je ne vois pas mes fichiers appsettings.School.json

C'est normal s'ils sont dans le `.gitignore`. Créez-les manuellement avec les commandes ci-dessus.

## 💡 Astuce

Vous pouvez vérifier quel environnement est actif en ajoutant ceci dans une page Blazor :

```razor
@inject IWebHostEnvironment Env

<p>Environnement actuel : @Env.EnvironmentName</p>
```
