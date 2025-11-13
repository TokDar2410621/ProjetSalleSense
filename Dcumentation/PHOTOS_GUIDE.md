# 📸 Guide du Système de Photos - SalleSense

## Vue d'ensemble

Le système de photos permet de stocker et afficher des images capturées par les caméras de surveillance dans les salles. Les images sont stockées en **BLOB (Binary Large OBject)** directement dans SQL Server.

---

## Architecture

### Base de données
```sql
Table: Donnees
├── idDonnee_PK      (INT)          -- ID unique
├── dateHeure        (DATETIME2)    -- Date de capture
├── idCapteur        (INT)          -- ID de la caméra
├── photoBlob        (VARBINARY)    -- Image en binaire
└── noSalle          (INT)          -- Salle associée
```

### Services C#
- **PhotoService.cs** - Logique métier pour gérer les BLOB
- **PhotoController.cs** - API REST pour servir les images
- **Photos.razor** - Interface utilisateur Blazor

---

## 🚀 Utilisation

### 1. Afficher les photos

**Page web :** Naviguez vers `/photos`

**Fonctionnalités :**
- ✅ Galerie avec miniatures
- ✅ Filtrage par salle
- ✅ Vue en grand (modal)
- ✅ Téléchargement
- ✅ Information de taille/date

### 2. API REST

#### Récupérer une image
```
GET /api/photo/{id}
Retourne: image/jpeg
```

Exemple :
```html
<img src="/api/photo/5" alt="Photo de la salle">
```

#### Liste des métadonnées
```
GET /api/photo/list
Retourne: JSON
```

Réponse :
```json
[
  {
    "idDonnee": 5,
    "dateHeure": "2025-11-13T10:30:00",
    "tailleBytes": 45678,
    "noSalle": 1,
    "idCapteur": 3,
    "tailleKB": 44.6,
    "tailleFormatee": "44.6 KB"
  }
]
```

#### Photos d'une salle
```
GET /api/photo/salle/{salleId}
Retourne: JSON
```

---

## 📥 Insertion de photos

### Méthode 1: Script SQL avec image test (1x1 pixel)

```bash
cd Script_bd
sqlcmd -S localhost -d Prog3A25_bdSalleSense -i InsertPhotosBlob.sql
```

Le script insère 5 photos de test (image JPG 1x1 pixel rouge).

### Méthode 2: Base64 via procédure stockée

**Étape 1 : Convertir votre image en Base64**

PowerShell :
```powershell
$bytes = [System.IO.File]::ReadAllBytes("C:\chemin\photo.jpg")
$base64 = [System.Convert]::ToBase64String($bytes)
Write-Output $base64
```

En ligne : https://base64.guru/converter/encode/image

**Étape 2 : Insérer via SQL**

```sql
EXEC dbo.usp_InsertPhotoFromBase64
    @Base64String = '/9j/4AAQSkZJRgABAQEA...',  -- Votre base64
    @IdCapteur = 3,                              -- CAM-1
    @NoSalle = 1;                                -- A-101
```

### Méthode 3: Via C# (PhotoService)

```csharp
// Dans votre code C#
byte[] photoBytes = File.ReadAllBytes("photo.jpg");

int idDonnee = await _photoService.InsertPhotoAsync(
    photoBytes,
    idCapteur: 3,   // CAM-1
    noSalle: 1      // A-101
);
```

### Méthode 4: Upload via API (à créer)

**Créer un endpoint d'upload :**

```csharp
// Dans PhotoController.cs
[HttpPost("upload")]
public async Task<IActionResult> UploadPhoto(
    [FromForm] IFormFile file,
    [FromForm] int idCapteur,
    [FromForm] int noSalle)
{
    if (file == null || file.Length == 0)
        return BadRequest("Aucun fichier fourni");

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    byte[] photoBytes = ms.ToArray();

    int idDonnee = await _photoService.InsertPhotoAsync(
        photoBytes, idCapteur, noSalle);

    return Ok(new { id = idDonnee });
}
```

**Utiliser avec curl :**
```bash
curl -X POST http://localhost:5000/api/photo/upload \
  -F "file=@photo.jpg" \
  -F "idCapteur=3" \
  -F "noSalle=1"
```

---

## 🔍 Vérification

### SQL : Vérifier les photos présentes

```sql
SELECT
    idDonnee_PK,
    dateHeure,
    noSalle,
    LEN(photoBlob) AS TailleBytes,
    LEN(photoBlob) / 1024.0 AS TailleKB
FROM Donnees
WHERE photoBlob IS NOT NULL
ORDER BY dateHeure DESC;
```

### C# : Tester le service

```csharp
// Dans votre code
var photos = await _photoService.GetAllPhotosAsync();
Console.WriteLine($"Nombre de photos: {photos.Count}");

foreach (var photo in photos)
{
    Console.WriteLine($"Photo #{photo.IdDonnee} - Salle {photo.NoSalle} - {photo.TailleFormatee}");
}
```

---

## 🐛 Résolution de problèmes

### Problème : "Aucune photo disponible"

**Cause :** La table `Donnees` ne contient pas de photos en BLOB.

**Solution :**
```sql
-- Vérifier
SELECT COUNT(*) FROM Donnees WHERE photoBlob IS NOT NULL;

-- Si 0, exécuter
cd Script_bd
sqlcmd -S localhost -d Prog3A25_bdSalleSense -i InsertPhotosBlob.sql
```

### Problème : Image ne s'affiche pas (404)

**Cause :** Le contrôleur API n'est pas accessible.

**Vérification :**
```bash
# Tester l'API
curl http://localhost:5000/api/photo/list

# Devrait retourner un JSON avec la liste des photos
```

**Solution :** Vérifier que `Startup.cs` contient :
```csharp
services.AddScoped<PhotoService>();
services.AddControllersWithViews();
```

### Problème : Image cassée (fichier corrompu)

**Cause :** Le BLOB est invalide ou incomplet.

**Vérification :**
```sql
-- Vérifier que les données ne sont pas vides
SELECT idDonnee_PK, LEN(photoBlob) AS Taille
FROM Donnees
WHERE photoBlob IS NOT NULL AND LEN(photoBlob) < 100;
```

**Solution :** Réinsérer la photo avec une image valide.

---

## 📊 Performance

### Optimisation des requêtes

```sql
-- Index sur noSalle pour filtrage rapide
CREATE NONCLUSTERED INDEX IX_Donnees_NoSalle_PhotoBlob
ON Donnees(noSalle)
WHERE photoBlob IS NOT NULL;

-- Index sur dateHeure pour tri chronologique
CREATE NONCLUSTERED INDEX IX_Donnees_DateHeure
ON Donnees(dateHeure DESC)
WHERE photoBlob IS NOT NULL;
```

### Taille maximale

SQL Server limite les VARBINARY(MAX) à **2 GB** par BLOB.

**Recommandation :** Compresser les images avant insertion.

**PowerShell - Compression JPG :**
```powershell
# Installer ImageMagick
# Puis compresser
magick convert input.jpg -quality 85 -resize 1920x1080 output.jpg
```

---

## 🔐 Sécurité

### Validation des images

**À implémenter dans PhotoController :**

```csharp
[HttpPost("upload")]
public async Task<IActionResult> UploadPhoto(IFormFile file)
{
    // 1. Vérifier l'extension
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
    var extension = Path.GetExtension(file.FileName).ToLower();
    if (!allowedExtensions.Contains(extension))
        return BadRequest("Format non autorisé");

    // 2. Vérifier la taille (max 5 MB)
    if (file.Length > 5 * 1024 * 1024)
        return BadRequest("Fichier trop volumineux");

    // 3. Vérifier le magic number (vraie image)
    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    var bytes = ms.ToArray();

    // JPEG: FF D8 FF
    if (bytes.Length < 3 || bytes[0] != 0xFF || bytes[1] != 0xD8)
        return BadRequest("Fichier corrompu");

    // OK, insérer
    await _photoService.InsertPhotoAsync(bytes, idCapteur, noSalle);
    return Ok();
}
```

---

## 🎯 Exemples d'utilisation

### Scénario 1: Surveillance automatique

```csharp
// Capturer une photo avec une caméra USB
using var camera = new VideoCaptureDevice();
byte[] photo = camera.CaptureFrame();

// Insérer dans la BD
await _photoService.InsertPhotoAsync(photo, idCapteur: 3, noSalle: 1);
```

### Scénario 2: Archive mensuelle

```csharp
// Récupérer toutes les photos d'une salle
var photos = await _photoService.GetPhotosBySalleAsync(1);

// Télécharger et sauvegarder localement
foreach (var photoInfo in photos)
{
    var bytes = await _photoService.GetPhotoByIdAsync(photoInfo.IdDonnee);
    var filename = $"salle_{photoInfo.NoSalle}_{photoInfo.DateHeure:yyyyMMdd_HHmmss}.jpg";
    File.WriteAllBytes(filename, bytes);
}
```

### Scénario 3: Détection de mouvement

```csharp
// Déclencher une capture quand mouvement détecté
if (mouvementDetecte)
{
    var photo = await camera.CaptureAsync();
    var idDonnee = await _photoService.InsertPhotoAsync(photo, 3, salleId);

    // Créer un événement associé
    var evenement = new Evenement
    {
        Type = "CAPTURE",
        IdDonnee = idDonnee,
        Description = $"Mouvement détecté - Photo capturée"
    };
    await context.Evenements.AddAsync(evenement);
    await context.SaveChangesAsync();
}
```

---

## 📚 Références

- **PhotoService.cs** : `/sallesense/Services/PhotoService.cs`
- **PhotoController.cs** : `/sallesense/Controllers/PhotoController.cs`
- **Photos.razor** : `/sallesense/Pages/Photos.razor`
- **Script SQL** : `/Script_bd/InsertPhotosBlob.sql`
- **Modèle Donnee** : `/sallesense/Models/Donnee.cs`

---

## ✅ Checklist de déploiement

- [ ] Scripts SQL exécutés (création tables + insertions)
- [ ] Script `InsertPhotosBlob.sql` exécuté (photos de test)
- [ ] Service enregistré dans `Startup.cs`
- [ ] API accessible : `GET /api/photo/list`
- [ ] Page accessible : `/photos`
- [ ] Images s'affichent correctement
- [ ] Filtrage par salle fonctionne
- [ ] Modal d'agrandissement opérationnel
- [ ] Téléchargement fonctionnel

---

**Dernière mise à jour :** 13 novembre 2025
**Version :** 1.0
