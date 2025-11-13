# DARIUS.md - Explication du AuthService ligne par ligne

Ce fichier explique le code de `AuthService.cs` de manière détaillée pour que tu comprennes chaque ligne.

---

## Table des matières
1. [Les imports (using)](#1-les-imports-using)
2. [La classe AuthService](#2-la-classe-authservice)
3. [Le constructeur](#3-le-constructeur)
4. [La méthode RegisterAsync](#4-la-méthode-registerasync)
5. [La méthode LoginAsync](#5-la-méthode-loginasync)
6. [Les méthodes utilitaires](#6-les-méthodes-utilitaires)
7. [Vocabulaire important](#7-vocabulaire-important)

---

## 1. Les imports (using)

```csharp
using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;
```

**Explication:**
- `using` = "J'ai besoin d'utiliser du code qui vient d'ailleurs"
- `System;` = Contient les classes de base comme Exception, DateTime, etc.
- `System.Data;` = Contient SqlDbType (pour dire quel type de données on envoie à SQL Server)
- `System.Threading.Tasks;` = Contient Task (pour le code asynchrone)
- `Microsoft.Data.SqlClient;` = Contient SqlParameter (pour parler avec SQL Server)
- `Microsoft.EntityFrameworkCore;` = Contient Entity Framework (pour accéder à la base de données)
- `SallseSense.Data;` = Contient ton DbContext (la connexion à ta base de données)

**Analogie:** C'est comme dire "J'ai besoin d'un marteau, d'une scie, d'un tournevis" avant de commencer à construire.

---

## 2. La classe AuthService

```csharp
namespace SallseSense.Services
{
    public class AuthService
    {
```

**Explication:**
- `namespace SallseSense.Services` = "Ce fichier fait partie du dossier/groupe Services"
- `public` = Tout le monde peut utiliser cette classe
- `class AuthService` = On crée une "boîte à outils" qui s'appelle AuthService

**Analogie:** C'est comme créer une boîte à outils spéciale pour l'authentification.

---

```csharp
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _factory;
```

**Explication mot par mot:**
- `private` = Seul ce fichier peut voir cette variable (personne d'autre)
- `readonly` = Une fois qu'on lui donne une valeur, on ne peut plus la changer
- `IDbContextFactory<...>` = C'est une "usine" qui crée des connexions à ta base de données
- `Prog3A25BdSalleSenseContext` = Le nom de ta base de données (celle que tu as dans Data/)
- `_factory` = Le nom de la variable (le underscore _ est une convention pour dire "c'est privé")

**Pourquoi ce nom?**
- On l'appelle `_factory` car c'est une usine (factory en anglais) qui fabrique des connexions
- Le `_` au début dit "c'est une variable privée de la classe"

**Analogie:** C'est comme avoir une machine qui fabrique des clés pour ouvrir ton coffre-fort (la base de données).

---

## 3. Le constructeur

```csharp
        public AuthService(IDbContextFactory<Prog3A25BdSalleSenseContext> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }
```

**Explication:**
- `public AuthService(...)` = C'est le constructeur, la fonction qui s'exécute quand on crée un AuthService
- `IDbContextFactory<...> factory` = On reçoit l'usine de connexions en paramètre

**Ligne importante:**
```csharp
_factory = factory ?? throw new ArgumentNullException(nameof(factory));
```

**Décomposition:**
- `_factory = factory` = On stocke l'usine dans notre variable privée
- `??` = "OU SINON" (opérateur de coalescence nulle)
- `throw new ArgumentNullException(...)` = Lance une erreur si factory est null
- `nameof(factory)` = Donne le nom "factory" pour le message d'erreur

**En français:** "Stocke factory dans _factory, MAIS si factory est null, lance une erreur"

**Pourquoi?** Pour éviter les bugs plus tard si quelqu'un oublie de passer l'usine.

---

## 4. La méthode RegisterAsync

### 4.1 Signature de la méthode

```csharp
public async Task<(bool success, int userId, string message)> RegisterAsync(
    string pseudo,
    string courriel,
    string motDePasse)
{
```

**Décomposition:**
- `public` = Tout le monde peut appeler cette méthode
- `async` = Cette méthode est asynchrone (elle peut attendre sans bloquer le programme)
- `Task<...>` = Elle retourne une "tâche" qui donnera éventuellement un résultat
- `(bool success, int userId, string message)` = Elle retourne 3 choses à la fois (un tuple):
  - `success` = true/false (ça a marché ou pas?)
  - `userId` = L'ID de l'utilisateur créé (ou -1 si erreur)
  - `message` = Un message pour l'utilisateur
- `RegisterAsync` = Le nom de la méthode (le "Async" dit qu'elle est asynchrone)
- `string pseudo, string courriel, string motDePasse` = Les 3 infos qu'on doit donner

**Pourquoi ces noms?**
- `success` = "succès" en français, c'est clair
- `userId` = "user" (utilisateur) + "Id" (identifiant)
- `message` = Le message qu'on va montrer à l'utilisateur
- `pseudo`, `courriel`, `motDePasse` = En français car c'est ta base de données qui les utilise

---

### 4.2 Validation des entrées

```csharp
// Validation des entrées
if (string.IsNullOrWhiteSpace(pseudo))
    return (false, -1, "Le pseudo est requis.");
```

**Explication:**
- `string.IsNullOrWhiteSpace(pseudo)` = Vérifie si pseudo est vide ou contient seulement des espaces
- `return (false, -1, "Le pseudo est requis.");` = On retourne immédiatement avec:
  - `false` = Échec
  - `-1` = Pas d'ID (car on n'a rien créé)
  - Un message d'erreur

**Analogie:** C'est comme vérifier qu'on a tous les ingrédients avant de cuisiner.

---

```csharp
if (motDePasse.Length < 6)
    return (false, -1, "Le mot de passe doit contenir au moins 6 caractères.");
```

**Explication:**
- `motDePasse.Length` = La longueur (nombre de caractères) du mot de passe
- `< 6` = Plus petit que 6
- On impose un minimum de sécurité

---

### 4.3 Le try-catch

```csharp
try
{
    // Code qui peut planter
}
catch (SqlException ex)
{
    // Si c'est une erreur SQL spécifique
}
catch (Exception ex)
{
    // Si c'est n'importe quelle autre erreur
}
```

**Explication:**
- `try { ... }` = "Essaie de faire ça"
- `catch (SqlException ex)` = "Si ça plante avec une erreur SQL, fais ça"
- `catch (Exception ex)` = "Si ça plante avec n'importe quelle erreur, fais ça"
- `ex` = La variable qui contient les détails de l'erreur

**Analogie:** C'est comme avoir un plan B et un plan C si quelque chose tourne mal.

---

### 4.4 Création du contexte

```csharp
await using var db = await _factory.CreateDbContextAsync();
```

**Décomposition:**
- `await` = "Attends que ça finisse avant de continuer"
- `using` = "Utilise cette ressource et nettoie automatiquement après"
- `var` = "Devine le type pour moi" (ici c'est Prog3A25BdSalleSenseContext)
- `db` = Le nom de la variable (database = base de données)
- `_factory.CreateDbContextAsync()` = Demande à l'usine de créer une nouvelle connexion

**Pourquoi "db"?**
- Court et simple
- Conventionnel (tout le monde comprend que c'est la database)

**Analogie:** C'est comme ouvrir une porte vers ta base de données, et `using` garantit qu'on fermera la porte après.

---

### 4.5 Création des paramètres SQL

```csharp
var pPseudo = new SqlParameter("@Pseudo", SqlDbType.NVarChar, 100) { Value = pseudo };
```

**Décomposition:**
- `var pPseudo` = Une variable pour le paramètre du pseudo (le "p" signifie "parameter")
- `new SqlParameter(...)` = Crée un nouveau paramètre SQL
- `"@Pseudo"` = Le nom du paramètre dans ta procédure stockée (avec @)
- `SqlDbType.NVarChar` = Le type de données SQL (texte Unicode variable)
- `100` = La longueur maximale (100 caractères)
- `{ Value = pseudo }` = La valeur réelle qu'on envoie

**Pourquoi "pPseudo"?**
- Le "p" dit "c'est un paramètre SQL"
- "Pseudo" correspond au nom dans ta base de données
- Convention: préfixe + nom descriptif

**Autres paramètres:**
```csharp
var pCourriel = new SqlParameter("@Courriel", SqlDbType.NVarChar, 255) { Value = courriel };
var pMdp = new SqlParameter("@MotDePasse", SqlDbType.NVarChar, 4000) { Value = motDePasse };
```
- `pCourriel` = Paramètre pour le courriel
- `pMdp` = Paramètre pour le mot de passe (abrégé car "motDePasse" est long)

---

### 4.6 Paramètre de retour

```csharp
var ret = new SqlParameter("@RETURN_VALUE", SqlDbType.Int)
{
    Direction = ParameterDirection.ReturnValue
};
```

**Explication:**
- `ret` = "return" abrégé, c'est la valeur que la procédure stockée va retourner
- `"@RETURN_VALUE"` = Nom **IMPOSÉ** par SQL Server pour les valeurs de retour
  - ⚠️ **Tu NE PEUX PAS changer ce nom!** Ce n'est pas `@retour` ou `@resultat`
  - C'est une **convention Microsoft** - ADO.NET cherche spécifiquement `@RETURN_VALUE`
  - Par contre, le nom de la variable C# (`ret`) est libre - tu peux l'appeler comme tu veux
- `SqlDbType.Int` = C'est un nombre entier
- `Direction = ParameterDirection.ReturnValue` = Dit "ce paramètre REÇOIT une valeur (il ne l'envoie pas)"

**🔍 Qu'est-ce que Direction?**

`Direction` indique dans **quelle direction** voyage le paramètre:

| Direction | Symbole | Signification | Exemple |
|-----------|---------|---------------|---------|
| `Input` | ⬇️ | C# → SQL (envoie seulement) | Envoyer un pseudo |
| `Output` | ⬆️ | SQL → C# (reçoit seulement) | Récupérer un ID généré |
| `InputOutput` | ⬇️⬆️ | C# ↔ SQL (envoie et reçoit) | Modifier un compteur |
| `ReturnValue` | 🔙 | Récupère le RETURN de la procédure | **TON CAS** |

**Exemple visuel:**
```
Direction.Input (par défaut)
var pPseudo = new SqlParameter("@Pseudo", ...) { Value = "Darius" }
   C# envoie "Darius" → SQL Server reçoit et utilise

Direction.Output
var pNewId = new SqlParameter("@NewId", ...) { Direction = Output }
   SQL Server calcule l'ID → C# reçoit le résultat

Direction.ReturnValue (ton cas)
var ret = new SqlParameter("@RETURN_VALUE", ...) { Direction = ReturnValue }
   SQL fait RETURN 42 → C# reçoit 42 dans ret.Value
```

**Différence OUTPUT vs RETURN:**
```sql
-- OUTPUT (dans les paramètres)
CREATE PROCEDURE usp_Test1
    @Resultat INT OUTPUT
AS
    SET @Resultat = 42

-- RETURN (code de retour)
CREATE PROCEDURE usp_Test2
AS
    RETURN 42
```

Dans ton code, tu utilises `ReturnValue` car ta procédure fait `RETURN @userId` ou `RETURN -1`

**Pourquoi @RETURN_VALUE?**
```csharp
// ❌ Ceci NE MARCHE PAS
var ret = new SqlParameter("@monRetour", SqlDbType.Int) { ... }

// ✅ Ceci MARCHE
var ret = new SqlParameter("@RETURN_VALUE", SqlDbType.Int) { ... }

// ✅ Variable C# différente mais paramètre SQL identique
var resultat = new SqlParameter("@RETURN_VALUE", SqlDbType.Int) { ... }
```

**Analogie:** C'est comme donner une boîte vide à quelqu'un et lui dire "mets le résultat dedans". Mais la boîte doit avoir une étiquette spécifique "@RETURN_VALUE" pour que SQL Server la reconnaisse.

---

### 4.7 Exécution de la procédure stockée

```csharp
var sql = "EXEC @RETURN_VALUE = dbo.usp_Utilisateur_Create @Pseudo, @Courriel, @MotDePasse";

await db.Database.ExecuteSqlRawAsync(sql, ret, pPseudo, pCourriel, pMdp);
```

**Ligne 1:**
- `var sql = "..."` = La commande SQL qu'on va exécuter
- `EXEC` = EXECute (exécute une procédure stockée)
- `@RETURN_VALUE = ` = Stocke le résultat dans ce paramètre
- `dbo.usp_Utilisateur_Create` = Le nom de ta procédure stockée
- `@Pseudo, @Courriel, @MotDePasse` = Les paramètres qu'on passe

**⚠️ IMPORTANT: Pourquoi pas de parenthèses `()` ?**

```sql
-- ✅ CORRECT en SQL
EXEC usp_Utilisateur_Create @Pseudo, @Courriel, @MotDePasse

-- ❌ ERREUR en SQL
EXEC usp_Utilisateur_Create(@Pseudo, @Courriel, @MotDePasse)
```

**Explication:**
- En SQL Server, les **procédures stockées n'utilisent JAMAIS de parenthèses** avec `EXEC`
- C'est une **règle de syntaxe SQL**, différente de C#
- En C#: `MaMethode(param1, param2)` ✅ avec parenthèses
- En SQL: `EXEC MaProcedure param1, param2` ✅ sans parenthèses

**Exception:** Les **fonctions SQL** utilisent des parenthèses:
```sql
-- Fonction SQL
SELECT dbo.fnCalculer(10, 20)  -- ✅ Avec parenthèses

-- Procédure SQL
EXEC usp_Calculer 10, 20        -- ✅ Sans parenthèses
```

**Différence:**
- **Procédure** = Action/Commande → Pas de parenthèses
- **Fonction** = Calcul/Retourne une valeur → Parenthèses

**Ligne 2:**
- `await` = Attends que ça finisse
- `db.Database` = Accède à la base de données
- `ExecuteSqlRawAsync(...)` = Exécute du SQL brut (raw = brut)
- `sql` = La commande SQL
- `ret, pPseudo, pCourriel, pMdp` = Tous les paramètres dans l'ordre

**Pourquoi cet ordre?**
- `ret` en premier car c'est le @RETURN_VALUE
- Puis les autres dans l'ordre où ils apparaissent dans la commande SQL

---

### 4.8 Récupération et traitement du résultat

```csharp
int userId = (int)(ret.Value ?? -1);
```

**Décomposition:**
- `int userId` = On déclare une variable entière
- `ret.Value` = La valeur que la procédure stockée a mise dans ret
- `?? -1` = "Si c'est null, utilise -1 à la place"
- `(int)(...)` = Convertit en int (au cas où)

**En français:** "Prends la valeur de ret, si elle est null utilise -1, et stocke ça dans userId"

---

```csharp
if (userId > 0)
{
    return (true, userId, "Inscription réussie!");
}
else if (userId == -1)
{
    return (false, -1, "Cette adresse courriel est déjà utilisée.");
}
else
{
    return (false, -1, "Une erreur est survenue lors de l'inscription.");
}
```

**Explication:**
- Ta procédure stockée retourne:
  - Un nombre **positif** = ID du nouvel utilisateur (succès!)
  - **-1** = Le courriel existe déjà (échec)
  - Autre chose = Erreur inconnue

**Convention de retour:**
- `(true, userId, message)` = Succès avec l'ID et un message
- `(false, -1, message)` = Échec avec un message d'erreur

---

### 4.9 Gestion des erreurs

```csharp
catch (SqlException ex)
{
    // Log l'erreur (à implémenter avec un logger)
    return (false, -1, $"Erreur de base de données: {ex.Message}");
}
```

**Explication:**
- `SqlException ex` = Une erreur spécifique à SQL Server
- `ex.Message` = Le message d'erreur
- `$"..."` = String interpolation (permet de mettre {ex.Message} dans le texte)

**Exemple de message:**
```
"Erreur de base de données: Connection timeout"
```

---

```csharp
catch (Exception ex)
{
    // Log l'erreur (à implémenter avec un logger)
    return (false, -1, $"Erreur inattendue: {ex.Message}");
}
```

**Explication:**
- Attrape **toutes** les autres erreurs qu'on n'a pas prévues
- Toujours mettre ça en dernier dans les catch

---

## 5. La méthode LoginAsync

Elle fonctionne **exactement comme RegisterAsync**, mais:

### Différences principales:

```csharp
public async Task<(bool success, int userId, string message)> LoginAsync(
    string courriel,
    string motDePasse)
```
- Seulement 2 paramètres (pas de pseudo)

---

```csharp
var pCourriel = new SqlParameter("@Courriel", SqlDbType.NVarChar, 255) { Value = courriel };
var pMdp = new SqlParameter("@MotDePasse", SqlDbType.NVarChar, 4000) { Value = motDePasse };
```
- Seulement 2 paramètres SQL

---

```csharp
var sql = "EXEC @RETURN_VALUE = dbo.usp_Utilisateur_Login @Courriel, @MotDePasse";
```
- Appelle une procédure différente: `usp_Utilisateur_Login`

---

```csharp
else if (userId == -2)
{
    return (false, -2, "Votre compte a été bloqué. Contactez l'administrateur.");
}
```
- **Nouveau cas:** -2 = Utilisateur blacklisté
- Ta procédure stockée doit retourner -2 si l'utilisateur est dans la Blacklist

**Convention de retour pour Login:**
- **> 0** = Succès, voici l'ID de l'utilisateur
- **-1** = Mauvais courriel ou mot de passe
- **-2** = Utilisateur blacklisté

---

## 6. Les méthodes utilitaires

### 6.1 IsUserBlacklistedAsync

```csharp
public async Task<bool> IsUserBlacklistedAsync(int userId)
{
    try
    {
        await using var db = await _factory.CreateDbContextAsync();

        var isBlacklisted = await db.Blacklists
            .AnyAsync(b => b.IdUtilisateur == userId);

        return isBlacklisted;
    }
    catch (Exception)
    {
        return false;
    }
}
```

**Explication ligne par ligne:**

```csharp
public async Task<bool> IsUserBlacklistedAsync(int userId)
```
- Retourne un `bool` = true ou false
- Prend un `userId` en paramètre

---

```csharp
await using var db = await _factory.CreateDbContextAsync();
```
- Même principe: ouvre une connexion à la BD

---

```csharp
var isBlacklisted = await db.Blacklists
    .AnyAsync(b => b.IdUtilisateur == userId);
```

**Décomposition:**
- `db.Blacklists` = Accède à la table Blacklist
- `.AnyAsync(...)` = "Est-ce qu'il existe au moins une ligne qui..."
- `b => b.IdUtilisateur == userId` = Une fonction lambda:
  - `b` = Une ligne de la table Blacklist
  - `b.IdUtilisateur` = La colonne IdUtilisateur de cette ligne
  - `== userId` = Est égal à l'userId qu'on cherche

**En français:** "Y a-t-il au moins une ligne dans Blacklist où IdUtilisateur = userId?"

**Résultat:**
- `true` = Oui, il est blacklisté
- `false` = Non, il n'est pas blacklisté

---

```csharp
catch (Exception)
{
    return false;
}
```
- Si ça plante, on dit "non il n'est pas blacklisté" (stratégie permissive)
- Autre stratégie possible: relancer l'erreur avec `throw;`

---

### 6.2 GetUserByIdAsync

```csharp
public async Task<Models.Utilisateur?> GetUserByIdAsync(int userId)
{
    try
    {
        await using var db = await _factory.CreateDbContextAsync();

        var user = await db.Utilisateurs
            .FirstOrDefaultAsync(u => u.IdUtilisateurPk == userId);

        return user;
    }
    catch (Exception)
    {
        return null;
    }
}
```

**Explication:**

```csharp
public async Task<Models.Utilisateur?> GetUserByIdAsync(int userId)
```
- `Models.Utilisateur?` = Retourne un objet Utilisateur **OU null** (le `?` indique que ça peut être null)

---

```csharp
var user = await db.Utilisateurs
    .FirstOrDefaultAsync(u => u.IdUtilisateurPk == userId);
```

**Décomposition:**
- `db.Utilisateurs` = La table Utilisateur
- `.FirstOrDefaultAsync(...)` = "Trouve le premier qui correspond, ou null si aucun"
- `u => u.IdUtilisateurPk == userId` = Lambda:
  - `u` = Un utilisateur
  - `u.IdUtilisateurPk` = Son ID
  - `== userId` = Correspond à l'ID qu'on cherche

**En français:** "Trouve le premier utilisateur dont l'ID est userId, sinon retourne null"

---

## 7. Vocabulaire important

### Termes de programmation:

| Terme | Explication | Exemple |
|-------|-------------|---------|
| **async/await** | Code asynchrone (n'attend pas, continue autre chose) | `await db.SaveChangesAsync()` |
| **Task** | Une "promesse" de résultat futur | `Task<int>` = Une tâche qui retournera un int |
| **using** | Utilise et nettoie automatiquement | `using var db = ...` ferme la connexion après |
| **var** | Le compilateur devine le type | `var x = 5;` → x est un int |
| **??** | Opérateur de coalescence nulle | `a ?? b` = "a si a n'est pas null, sinon b" |
| **Lambda (=>)** | Fonction anonyme courte | `x => x > 5` = "x tel que x est > 5" |
| **Tuple** | Grouper plusieurs valeurs | `(bool, int, string)` = 3 valeurs ensemble |
| **?** (type nullable) | Peut être null | `int?` = un int OU null |

---

### Noms de variables couramment utilisés:

| Nom | Signification | Pourquoi ce nom? |
|-----|---------------|------------------|
| **db** | Database (base de données) | Court et universel |
| **ex** | Exception (erreur) | Abréviation standard |
| **ret** | Return (retour) | Ce qui est retourné |
| **p[Nom]** | Parameter (paramètre SQL) | pPseudo, pCourriel, etc. |
| **_factory** | Usine privée | `_` = privé, factory = usine |
| **userId** | User ID (identifiant utilisateur) | Camel case: user + Id |

---

### Conventions de nommage C#:

| Type | Convention | Exemple |
|------|------------|---------|
| **Classe** | PascalCase | `AuthService` |
| **Méthode** | PascalCase | `RegisterAsync` |
| **Variable locale** | camelCase | `userId`, `isBlacklisted` |
| **Paramètre** | camelCase | `pseudo`, `courriel` |
| **Champ privé** | _camelCase | `_factory` |
| **Constante** | UPPER_CASE | `MAX_LENGTH` |

---

## 8. Schéma de fonctionnement

### Flux RegisterAsync:

```
1. Utilisateur entre pseudo, courriel, mot de passe
   ↓
2. Validation (vide? trop court?)
   ↓
3. Création de la connexion BD (db)
   ↓
4. Préparation des paramètres SQL
   ↓
5. Exécution de la procédure stockée
   ↓
6. Récupération du résultat
   ↓
7. Interprétation:
   - > 0 ? → Succès!
   - -1 ? → Courriel déjà utilisé
   - Autre ? → Erreur
   ↓
8. Retour du tuple (success, userId, message)
```

---

### Flux LoginAsync:

```
1. Utilisateur entre courriel, mot de passe
   ↓
2. Validation (vide?)
   ↓
3. Création de la connexion BD (db)
   ↓
4. Préparation des paramètres SQL
   ↓
5. Exécution de la procédure stockée
   ↓
6. Récupération du résultat
   ↓
7. Interprétation:
   - > 0 ? → Succès! Voici l'userId
   - -1 ? → Mauvais identifiants
   - -2 ? → Blacklisté
   ↓
8. Retour du tuple (success, userId, message)
```

---

## 9. Questions fréquentes

### Q: Pourquoi utiliser des tuples au lieu de créer une classe?

**Réponse:**
```csharp
// Avec tuple (simple)
return (true, 42, "Succès!");

// Sans tuple (plus lourd)
public class AuthResult
{
    public bool Success { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; }
}
return new AuthResult { Success = true, UserId = 42, Message = "Succès!" };
```

Les tuples sont plus rapides pour des retours simples. Si c'était plus complexe, on créerait une classe.

---

### Q: Pourquoi "Async" à la fin du nom?

**Réponse:**
C'est une convention C#. Toute méthode asynchrone devrait avoir "Async" dans son nom:
- `RegisterAsync()` ✅
- `Register()` ❌ (on ne sait pas qu'elle est async)

---

### Q: C'est quoi la différence entre "using" et "await using"?

**Réponse:**
```csharp
// using classique (synchrone)
using var fichier = File.Open("data.txt");

// await using (asynchrone)
await using var db = await _factory.CreateDbContextAsync();
```

`await using` permet de nettoyer de manière asynchrone (meilleur pour les connexions BD).

---

### Q: Pourquoi SqlDbType.NVarChar et pas juste "string"?

**Réponse:**
SQL Server a besoin de savoir le type EXACT:
- `NVarChar` = Texte Unicode (supporte français, chinois, etc.)
- `VarChar` = Texte ASCII seulement
- `Int` = Nombre entier
- Etc.

Si on disait juste "string", SQL Server ne saurait pas s'il faut 10 ou 1000 caractères.

---

### Q: C'est quoi le rapport entre AuthService et ta procédure stockée?

**Réponse:**

```
AuthService.cs (C#)              Base de données SQL Server
     ↓                                    ↓
RegisterAsync()           →      usp_Utilisateur_Create
     ↓                                    ↓
Envoie:                          Reçoit:
- @Pseudo                        - @Pseudo
- @Courriel                      - @Courriel
- @MotDePasse                    - @MotDePasse
     ↓                                    ↓
Attend le résultat         ←     Retourne userId ou -1
     ↓
Interprète et retourne tuple
```

Le AuthService est juste un "intermédiaire" entre ton code C# et ta base de données.

---

## 10. Exercice pratique

Pour vérifier que tu as compris, essaie de répondre à ces questions:

1. **Que retourne RegisterAsync si le mot de passe fait 4 caractères?**
   <details>
   <summary>Réponse</summary>

   `(false, -1, "Le mot de passe doit contenir au moins 6 caractères.")`
   </details>

2. **Que signifie `_` devant `_factory`?**
   <details>
   <summary>Réponse</summary>

   C'est une variable privée de la classe (convention)
   </details>

3. **Pourquoi utilise-t-on `await using` au lieu de juste `using`?**
   <details>
   <summary>Réponse</summary>

   Pour nettoyer la connexion de manière asynchrone (meilleur pour les performances)
   </details>

4. **Si la procédure stockée retourne 5, que se passe-t-il?**
   <details>
   <summary>Réponse</summary>

   Succès! L'utilisateur a l'ID 5. On retourne `(true, 5, "Inscription réussie!")`
   </details>

---

## 11. Le Logging - Enregistrer ce qui se passe

### C'est quoi un "logger"?

Un **logger** est comme la **boîte noire d'un avion** - il enregistre tout ce qui se passe dans ton application pour que tu puisses comprendre ce qui s'est passé en cas de problème.

**Sans logger:**
```csharp
catch (Exception ex)
{
    return (false, -1, $"Erreur: {ex.Message}");
}
// L'erreur est perdue! Tu ne sauras jamais ce qui s'est passé.
```

**Avec logger:**
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Erreur lors de l'inscription de {Pseudo}", pseudo);
    return (false, -1, $"Erreur: {ex.Message}");
}
// L'erreur est ENREGISTRÉE avec tous les détails!
```

---

### Pourquoi c'est important?

**Scénario réel:**
- Un utilisateur t'appelle: "Ça marche pas!"
- Sans logger → Tu dois deviner 🤷
- Avec logger → Tu ouvres le fichier de logs et tu vois exactement ce qui s'est passé

**Exemple de log:**
```
[2025-01-30 14:32:15] ERROR: Erreur SQL lors de l'inscription. Pseudo: "Darius", Courriel: "darius@test.com"
SqlException: Connection timeout
   at SallseSense.Services.AuthService.RegisterAsync() in AuthService.cs:line 125
```

Tu sais **immédiatement** que c'est un problème de connexion à la base de données!

---

### Les niveaux de log

```csharp
_logger.LogTrace("Détails techniques (très verbeux)");
_logger.LogDebug("Info pour déboguer (développement)");
_logger.LogInformation("Info générale (ex: utilisateur connecté)");
_logger.LogWarning("Attention, quelque chose d'anormal");
_logger.LogError(ex, "Erreur qui empêche une action");
_logger.LogCritical(ex, "Erreur GRAVE qui peut crasher l'app");
```

---

### Comment l'utiliser?

**1. Ajoute ILogger dans le constructeur:**

```csharp
private readonly ILogger<AuthService> _logger;

public AuthService(
    IDbContextFactory<Prog3A25BdSalleSenseContext> factory,
    ILogger<AuthService> logger)  // ← Ajoute ce paramètre
{
    _factory = factory;
    _logger = logger;  // ← Stocke le logger
}
```

**2. Utilise-le dans tes méthodes:**

```csharp
public async Task<(bool success, int userId, string message)> LoginAsync(...)
{
    _logger.LogInformation("Tentative de connexion pour {Courriel}", courriel);

    try
    {
        // ... ton code ...

        if (userId > 0)
        {
            _logger.LogInformation("Connexion réussie pour userId {UserId}", userId);
            return (true, userId, "Connexion réussie!");
        }
        else
        {
            _logger.LogWarning("Échec de connexion pour {Courriel}", courriel);
            return (false, -1, "Identifiants invalides.");
        }
    }
    catch (SqlException ex)
    {
        _logger.LogError(ex, "Erreur SQL lors du login de {Courriel}", courriel);
        return (false, -1, $"Erreur: {ex.Message}");
    }
}
```

---

### Syntaxe du logger

```csharp
_logger.LogError(ex, "Message avec {Parametre1} et {Parametre2}", valeur1, valeur2);
                 ↑    ↑                                            ↑
                 │    │                                            └─ Valeurs (dans l'ordre)
                 │    └─ Message avec des placeholders {NomVariable}
                 └─ L'exception (peut être null pour Info/Warning)
```

**Exemple:**
```csharp
string pseudo = "Darius";
string courriel = "test@example.com";

_logger.LogError(ex,
    "Erreur lors de l'inscription. Pseudo: {Pseudo}, Courriel: {Courriel}",
    pseudo, courriel);
```

**Résultat dans le log:**
```
[2025-01-30 14:32:15] ERROR: Erreur lors de l'inscription. Pseudo: "Darius", Courriel: "test@example.com"
SqlException: Connection timeout
Stack trace: ...
```

---

### Où vont les logs?

**Par défaut:** Dans la **Console** (fenêtre noire quand tu lances l'app)

**Pour les écrire dans un fichier:** Utilise Serilog (package NuGet)

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
```

Puis configure dans `Program.cs`:
```csharp
builder.Host.UseSerilog((context, configuration) =>
    configuration.WriteTo.File("logs/app.txt", rollingInterval: RollingInterval.Day));
```

Les logs iront dans `logs/app-2025-01-30.txt`, `logs/app-2025-01-31.txt`, etc.

---

### Quoi logger?

**✅ À LOGGER:**
- Tentatives de connexion (succès et échecs)
- Erreurs SQL ou réseau
- Actions importantes (création compte, modification données)
- Warnings (capacité dépassée, tentative suspecte)

**❌ NE JAMAIS LOGGER:**
- Les mots de passe!
- Les numéros de carte de crédit
- Les données personnelles sensibles

**Exemple:**
```csharp
// ❌ DANGER!
_logger.LogInformation("Login avec mot de passe: {MotDePasse}", motDePasse);

// ✅ CORRECT
_logger.LogInformation("Login pour {Courriel}", courriel);
```

---

### Utilité en production

**Sans logs:**
- Client: "Ça marche pas depuis hier!"
- Toi: "Euh... je vais voir..." 🤷
- Tu perds des heures à chercher

**Avec logs:**
- Tu ouvres `logs/app-2025-01-29.txt`
- Tu cherches l'erreur
- Tu vois: `[23:45:12] ERROR: SqlException - Database full`
- Solution: Nettoyer la base de données
- **Problème résolu en 5 minutes!**

---

### En résumé

**Un logger c'est:**
- Un système qui **enregistre** ce qui se passe
- Comme une **boîte noire** d'avion
- **Essentiel** pour le debugging en production

**Comment l'utiliser:**
1. Ajoute `ILogger<AuthService>` dans le constructeur
2. Utilise `_logger.LogError()`, `_logger.LogInformation()`, etc.
3. Mets des messages clairs avec des variables
4. N'oublie JAMAIS de logger les erreurs dans les `catch`

**Règle d'or:**
> "Si ça peut planter, ça doit être loggé!"

---

## Conclusion

Ce fichier AuthService fait 3 choses principales:

1. **RegisterAsync()** = Inscrire un nouvel utilisateur
2. **LoginAsync()** = Connecter un utilisateur existant
3. **Méthodes utilitaires** = Vérifier blacklist, récupérer un user

Tout le code utilise:
- Des **paramètres SQL** pour éviter les injections SQL
- Des **tuples** pour retourner plusieurs valeurs
- Du **code asynchrone** pour ne pas bloquer l'application
- De la **validation** pour éviter les erreurs

Les noms sont choisis pour être:
- **Courts** mais **descriptifs**
- **Conventionnels** (suivent les standards C#)
- **Clairs** sur leur rôle

Si tu as d'autres questions, n'hésite pas! 🚀
