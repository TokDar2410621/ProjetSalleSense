# 📁 Nouvelle Approche: Sauvegarde Fichier → BD

## 🎯 Changement de Stratégie

### ❌ Ancienne Approche (PROBLÉMATIQUE)
```
Camera → BytesIO → bytes en mémoire → BD
         (Mode simulation: texte brut)
```

**Problème**: En mode simulation, on insérait du texte brut (`b"TEST_PHOTO_"`) au lieu d'une vraie image.

### ✅ Nouvelle Approche (COMME inserer_screenshots.py)
```
Camera → Fichier sur disque → Lecture avec open('rb') → BD
         (Mode simulation: vraie image PIL)
```

**Avantage**: **EXACTEMENT la même méthode que `inserer_screenshots.py` qui fonctionne!**

---

## 🔄 Flux Modifié

### 1. Capture de la Photo

```python
def capturer_photo(self) -> str:
    """Retourne le CHEMIN du fichier (pas les bytes)"""

    filepath = "photos_captures/photo_20251127_123456_1.jpg"

    if CAMERA_AVAILABLE:
        # Caméra réelle
        self.camera.capture_file(filepath)
    else:
        # Simulation: créer une VRAIE image PIL
        img = Image.new('RGB', (640, 480), color=(R, G, B))
        img.save(filepath, 'JPEG')

    return filepath  # Retourne le chemin
```

### 2. Envoi à la BD

```python
def envoyer_photo_bd(self, photo_path: str) -> bool:
    """EXACTEMENT comme inserer_screenshots.py"""

    # Lire la photo (ligne 113-114 de inserer_screenshots.py)
    with open(photo_path, 'rb') as file:
        photo_blob = file.read()

    # Créer un nouveau cursor (ligne 121)
    cursor = self.db.connection.cursor()

    # Insérer (lignes 131-137)
    cursor.execute(query, (id_capteur, photo_blob, no_salle))
    self.db.connection.commit()

    # Récupérer l'ID (lignes 141-142)
    cursor.execute("SELECT @@IDENTITY")
    id_donnee = cursor.fetchone()[0]

    # Fermer le cursor (ligne 150)
    cursor.close()
```

---

## 📊 Comparaison avec inserer_screenshots.py

| Étape | inserer_screenshots.py | capture_photos_continu.py (NOUVEAU) | Match? |
|-------|------------------------|-------------------------------------|--------|
| Lecture fichier | `with open(path, 'rb')` | `with open(path, 'rb')` | ✅ |
| Créer cursor | `conn.cursor()` | `self.db.connection.cursor()` | ✅ |
| Query SQL | `VALUES (GETDATE(), ?, NULL, ?, ?)` | `VALUES (GETDATE(), ?, NULL, ?, ?)` | ✅ |
| Execute | `cursor.execute(query, (id, blob, salle))` | `cursor.execute(query, (id, blob, salle))` | ✅ |
| Commit | `conn.commit()` | `self.db.connection.commit()` | ✅ |
| Get ID | `SELECT @@IDENTITY` | `SELECT @@IDENTITY` | ✅ |
| Fermer | `cursor.close()` | `cursor.close()` | ✅ |

**Résultat**: 100% identique!

---

## 🆕 Modifications Apportées

### Fichier: `capture_photos_continu.py`

#### 1. Import `os` (ligne 7)
```python
import os
```

#### 2. Paramètre `dossier_photos` dans `__init__` (lignes 24-44)
```python
def __init__(self, ..., dossier_photos: str = "photos_captures"):
    self.dossier_photos = dossier_photos

    # Créer le dossier
    if not os.path.exists(self.dossier_photos):
        os.makedirs(self.dossier_photos)
```

#### 3. `capturer_photo()` retourne un chemin (lignes 97-139)
```python
def capturer_photo(self) -> str:
    """Returns: filepath (not bytes)"""

    filepath = os.path.join(self.dossier_photos, filename)

    if CAMERA_AVAILABLE:
        self.camera.capture_file(filepath)
    else:
        # Simulation: créer une VRAIE image
        img = Image.new('RGB', (640, 480), ...)
        img.save(filepath, 'JPEG')

    return filepath
```

#### 4. `envoyer_photo_bd()` lit depuis le fichier (lignes 141-203)
```python
def envoyer_photo_bd(self, photo_path: str) -> bool:
    """Args: photo_path (not photo_bytes)"""

    # Lire depuis le fichier
    with open(photo_path, 'rb') as file:
        photo_blob = file.read()

    # Reste identique à inserer_screenshots.py
    ...
```

#### 5. Boucle principale (lignes 217-228)
```python
photo_path = self.capturer_photo()  # Chemin
if photo_path:
    self.envoyer_photo_bd(photo_path)  # Lit le fichier
```

---

## ✅ Avantages de Cette Approche

### 1. **Copie Exacte d'une Méthode qui Fonctionne**
- `inserer_screenshots.py` fonctionne → on utilise la même logique
- Moins de risque d'erreur

### 2. **Photos Sauvegardées Localement**
- Backup automatique dans `photos_captures/`
- Permet de vérifier visuellement les photos
- Debugging plus facile

### 3. **Mode Simulation Réaliste**
- Génère de VRAIES images PIL
- Images différentes à chaque fois (couleur aléatoire)
- Texte overlay pour identifier les simulations

### 4. **Structure de Noms de Fichiers**
```
photos_captures/
├── photo_20251127_092430_1.jpg
├── photo_20251127_092435_2.jpg
├── photo_20251127_092440_3.jpg
└── ...
```

Format: `photo_YYYYMMDD_HHMMSS_N.jpg`

---

## 🧪 Test

```bash
cd pythonRAs

# Lancer la capture (simulation)
python capture_photos_continu.py
```

**Résultat attendu**:
```
📷 Intervalle: 5 secondes
🏢 Salle: 1
💾 Stockage: Base de données (VARBINARY)

[12:34:56] Photo #1 envoyée (15.2 KB) - ID: 277 - photo_20251127_123456_1.jpg
[12:35:01] Photo #2 envoyée (15.8 KB) - ID: 278 - photo_20251127_123501_2.jpg
[12:35:06] Photo #3 envoyée (15.1 KB) - ID: 279 - photo_20251127_123506_3.jpg
...
```

**Vérification**:
```bash
# 1. Vérifier les fichiers locaux
ls -lh photos_captures/

# 2. Ouvrir une photo pour vérifier
eog photos_captures/photo_*.jpg  # ou xdg-open

# 3. Vérifier dans la BD
python debug_photo_blob.py
```

---

## 🔍 Debugging

### Si ça échoue encore:

1. **Vérifier les fichiers locaux**
   ```bash
   ls photos_captures/
   file photos_captures/photo*.jpg
   ```

2. **Tester la lecture manuelle**
   ```python
   with open('photos_captures/photo_xxx.jpg', 'rb') as f:
       data = f.read()
       print(f"Taille: {len(data)} bytes")
       print(f"Magic: {' '.join(f'{b:02X}' for b in data[:4])}")
   ```

3. **Comparer avec inserer_screenshots.py**
   ```bash
   # Utiliser inserer_screenshots sur les photos capturées
   python inserer_screenshots.py
   # Choisir le dossier: photos_captures/
   ```

---

## 📝 Différences Clés vs Ancien Code

| Aspect | Ancien | Nouveau |
|--------|--------|---------|
| Type de retour `capturer_photo()` | `bytes` | `str` (filepath) |
| Paramètre `envoyer_photo_bd()` | `photo_bytes: bytes` | `photo_path: str` |
| Lecture des données | En mémoire (BytesIO) | Depuis fichier (`open('rb')`) |
| Mode simulation | Texte brut | Vraie image PIL |
| Backup local | ❌ Non | ✅ Oui |

---

## ✨ Résumé

**Ce changement aligne capture_photos_continu.py avec inserer_screenshots.py**

- ✅ Même méthode de lecture (open + rb)
- ✅ Mêmes appels pyodbc
- ✅ Même gestion du cursor
- ✅ Vraies images en simulation
- ✅ Backup local automatique

**Si ça ne marche toujours pas, le problème est ailleurs (config BD, permissions, etc.)**

---

**Date**: 2025-11-27
**Version**: v3.0 (approche fichier)
**Status**: ✅ Prêt à tester
