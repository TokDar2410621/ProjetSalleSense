# Session du 19 novembre 2025 - Implémentation de l'affichage des photos BLOB

## Résumé
Cette session a permis de résoudre le problème d'affichage des photos stockées en BLOB dans la base de données et de les afficher correctement dans l'application Blazor.

## Problèmes identifiés et résolus

### 1. Page d'accueil - Problèmes de contraste (RÉSOLU ✓)

**Problème :**
- Statistiques avec fond bleu rendant le texte illisible
- Section CTA avec couleur violette non désirée

**Solution appliquée :**
- **Fichier modifié :** `sallesense/Pages/Index.razor`
- Changement du fond des statistiques de dégradé bleu vers `var(--gray-50)` (gris clair)
- Cartes de statistiques en blanc avec ombres et bordures
- Labels des stats en `var(--gray-600)` pour meilleur contraste
- Section CTA changée de violet vers dégradé bleu (`#1e40af` vers `#3b82f6`)

### 2. Script d'insertion de photos BLOB (CRÉÉ ✓)

**Problème :** Besoin d'un script pour tester l'insertion de photos dans la BD

**Solution créée :**
- **Fichier créé :** `pythonRAs/inserer_screenshots.py`
- Connexion à LocalDB avec Windows Authentication
- Multi-driver fallback (ODBC 17, 18, 13, Native Client, SQL Server)
- Lecture de screenshots depuis `C:\Users\Darius\Pictures\Screenshots`
- Insertion correcte avec tous les champs requis :
  ```sql
  INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
  VALUES (GETDATE(), ?, NULL, ?, ?)
  ```
- `mesure = NULL` pour les photos de caméra
- `noSalle = 1` par défaut
- Limitation à 5 Mo par photo
- Par défaut : 10 screenshots insérés

**Résultat :** 8 photos insérées avec succès dans la BD

### 3. Script de diagnostic des photos (CRÉÉ ✓)

**Fichiers créés :**
- `pythonRAs/test_photos_bd.py` - Diagnostic complet des photos
- `pythonRAs/verif_quick.py` - Vérification rapide
- `pythonRAs/check_photos.sql` - Requêtes SQL de diagnostic

**Fonctionnalités :**
- Comptage des photos dans la BD
- Vérification des tailles de BLOB
- Détection de format (JPEG vs PNG)
- Extraction de test pour validation
- Export d'une photo vers `test_extraction/` pour vérifier l'intégrité

### 4. PhotoController - Détection automatique du format (AMÉLIORÉ ✓)

**Problème :** Le contrôleur retournait toujours `image/jpeg` même pour des PNG

**Solution appliquée :**
- **Fichier modifié :** `sallesense/Controllers/PhotoController.cs`
- Détection des "magic bytes" pour identifier le format :
  - PNG : `0x89 0x50 0x4E 0x47` → `image/png`
  - JPEG : `0xFF 0xD8` → `image/jpeg`
- Ajout de logs détaillés pour le débogage
- Code aux lignes 41-57

### 5. PhotoService - Erreur LINQ critique (RÉSOLU ✓)

**Problème :**
```
Exception: System.InvalidOperationException:
The LINQ expression 'd.PhotoBlob.Length' could not be translated.
```

**Cause :** Entity Framework ne peut pas traduire `.PhotoBlob.Length` en SQL

**Solution appliquée :**
- **Fichier modifié :** `sallesense/Services/PhotoService.cs`
- Modification de la stratégie de requête :
  1. Récupération des données avec `ToListAsync()` (en mémoire)
  2. Filtrage et projection en mémoire où `.Length` fonctionne
- Méthodes modifiées :
  - `GetAllPhotosAsync()` (lignes 50-67)
  - `GetPhotosBySalleAsync()` (lignes 123-146)
- Ajout de logs détaillés pour le monitoring

**Code avant (ERREUR) :**
```csharp
var photos = await context.Donnees
    .Where(d => d.PhotoBlob != null && d.PhotoBlob.Length > 0) // ❌ ERREUR ICI
    .Select(d => new PhotoInfo { ... })
    .ToListAsync();
```

**Code après (CORRECT) :**
```csharp
var donnees = await context.Donnees
    .Where(d => d.PhotoBlob != null)
    .ToListAsync(); // ✓ Récupération d'abord

var photos = donnees
    .Where(d => d.PhotoBlob.Length > 0) // ✓ Filtrage en mémoire
    .Select(d => new PhotoInfo { ... })
    .ToList();
```

### 6. Page Photos - Affichage des galeries (RÉSOLU ✓)

**Problème :** Page affichait "Aucune photo disponible" malgré 8 photos dans la BD

**Cause :** Erreur LINQ dans PhotoService (voir point 5)

**Résultat :** Après correction du PhotoService, les 8 photos s'affichent correctement avec :
- Miniatures cliquables
- Détails (date, heure, taille)
- Modal pour agrandissement
- Téléchargement possible
- Filtre par salle

### 7. SalleDetails - Affichage des photos dans la sidebar (IMPLÉMENTÉ ✓)

**Problème :** Section "Photo archive" affichait des icônes au lieu de vraies images

**Solution appliquée :**
- **Fichiers modifiés :**
  - `sallesense/Pages/SalleDetails.razor.cs`
  - `sallesense/Pages/SalleDetails.razor`

**Modifications code-behind (SalleDetails.razor.cs) :**
- Ajout de `IdDonnee` dans `PhotoViewModel` (ligne 130)
- Récupération de l'ID lors du chargement (lignes 88-94)
- Filtrage des photos avec BLOB non vide

**Modifications affichage (SalleDetails.razor) :**
- Remplacement de l'icône par `<img src="/api/photo/@photo.IdDonnee">`
- Style : `height: 120px; object-fit: cover`
- Affichage de 6 photos maximum
- Lien "Voir toutes" vers la page Photos
- Code aux lignes 151-172

## Architecture technique

### Base de données
- **Table :** `Donnees`
- **Colonnes clés :**
  - `idDonnee_PK` (int) - Clé primaire auto-incrémentée
  - `dateHeure` (datetime2) - Date/heure de capture
  - `idCapteur` (int) - FK vers Capteur
  - `mesure` (float, nullable) - NULL pour photos, valeur pour capteurs audio/mouvement
  - `photoBlob` (varbinary(max), nullable) - Données binaires de l'image
  - `noSalle` (int) - FK vers Salle

### API REST
- **Endpoint :** `GET /api/photo/{id}`
- **Retour :** Image avec Content-Type détecté automatiquement
- **Formats supportés :** JPEG, PNG
- **Logs :** Type et taille de chaque photo servie

### Services Blazor
- **PhotoService :** Récupération des métadonnées et BLOBs
- **PhotoController :** API pour servir les images
- **Pages :** Photos (galerie complète), SalleDetails (miniatures)

## Environnements

### Base de données utilisée
- **Development :** `Server=localhost` (SQL Server complet)
- **Home :** `Server=(localdb)\MSSQLLocalDB` (LocalDB - UTILISÉ POUR CETTE SESSION)

**Important :** Les photos ont été insérées dans LocalDB, donc l'application doit tourner en mode `Home` :
```bash
$env:ASPNETCORE_ENVIRONMENT="Home"
dotnet run
```

## Statistiques

### Photos insérées
- **Total :** 8 photos
- **Salle :** Salle 1 (toutes)
- **Capteur :** ID 8 (type CAMERA)
- **Tailles :** Entre 83 KB et 196 KB
- **Format :** PNG (screenshots Windows)
- **Dates :** 19/11/2025 19:32

### Code modifié
- **Fichiers C# modifiés :** 3
  - `Controllers/PhotoController.cs`
  - `Services/PhotoService.cs`
  - `Pages/SalleDetails.razor.cs`
- **Fichiers Razor modifiés :** 2
  - `Pages/Index.razor`
  - `Pages/SalleDetails.razor`
- **Scripts Python créés :** 3
  - `inserer_screenshots.py`
  - `test_photos_bd.py`
  - `verif_quick.py`
- **Scripts SQL créés :** 1
  - `check_photos.sql`

## Points d'attention

### Entity Framework et LINQ
⚠️ **Limitation critique :** Ne jamais utiliser `.Length` sur `byte[]` dans une requête LINQ to SQL
- ❌ Mauvais : `.Where(d => d.PhotoBlob.Length > 0).ToListAsync()`
- ✓ Bon : `.ToListAsync()` puis `.Where(d => d.PhotoBlob.Length > 0).ToList()`

### Configuration de connexion
- Vérifier quel `appsettings.{Environment}.json` est actif
- LocalDB vs SQL Server complet ont des données différentes
- Utiliser `launchSettings.json` pour configurer l'environnement

### Performance
- Les BLOBs sont chargés en mémoire pour filtrage
- Limite de 10 photos dans SalleDetails pour éviter surcharge
- Logs détaillés activés pour monitoring

## Tests effectués

✓ Insertion de 8 photos dans LocalDB
✓ Récupération des photos via PhotoService
✓ Affichage dans la page Photos (galerie complète)
✓ Affichage dans SalleDetails (6 miniatures)
✓ Modal d'agrandissement fonctionnel
✓ Détection automatique PNG/JPEG
✓ Téléchargement des photos
✓ Filtrage par salle

## Commandes utiles

### Insertion de photos (Python)
```bash
cd pythonRAs
.env\Scripts\activate
python inserer_screenshots.py
```

### Diagnostic des photos
```bash
python verif_quick.py
python test_photos_bd.py
```

### Lancement de l'application
```bash
cd sallesense
$env:ASPNETCORE_ENVIRONMENT="Home"
dotnet run
```

### Accès à la galerie
- Galerie complète : `https://localhost:5001/photos`
- Détails salle 1 : `https://localhost:5001/salle/1`

## Prochaines étapes suggérées

1. Ajouter pagination sur la page Photos (si > 50 photos)
2. Implémenter la suppression de photos
3. Ajouter filtres avancés (date, capteur, taille)
4. Optimiser le chargement des miniatures (thumbnails)
5. Ajouter upload manuel de photos
6. Implémenter rotation/recadrage d'images
7. Statistiques d'utilisation du stockage

## Liens vers le code

- [PhotoController.cs:30-63](sallesense/Controllers/PhotoController.cs#L30-L63)
- [PhotoService.cs:32-78](sallesense/Services/PhotoService.cs#L32-L78)
- [SalleDetails.razor:151-172](sallesense/Pages/SalleDetails.razor#L151-L172)
- [inserer_screenshots.py](pythonRAs/inserer_screenshots.py)

---

**Session réalisée le :** 19 novembre 2025
**Durée estimée :** ~2h
**Statut :** ✓ Tous les objectifs atteints
