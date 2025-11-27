# 🧪 Tests de Debugging - Photos BLOB

## Scripts Créés pour le Debugging

### 1. `debug_photo_blob.py` - Analyse Complète ⭐

**Objectif**: Analyser les photos existantes dans la BD et tester le cycle complet

**Ce qu'il fait**:
- ✅ Analyse toutes les photos dans la BD
- ✅ Affiche les statistiques (taille, format, magic bytes)
- ✅ Teste le décodage avec PIL
- ✅ Sauvegarde les photos décodables
- ✅ Test cycle complet: créer image → insérer → récupérer → décoder → comparer

**Exécution**:
```bash
cd pythonRAs
python debug_photo_blob.py
```

**Sortie attendue**:
- Statistiques globales (nb photos, tailles)
- Liste des 10 dernières photos avec détails
- Magic bytes et format détecté
- Test de décodage PIL
- Photos sauvegardées dans `test_photos_debug/`
- Test cycle complet avec comparaison binaire

---

### 2. `test_simple_insertion.py` - Test Comparatif

**Objectif**: Comparer la méthode corrigée vs ancienne méthode

**Ce qu'il fait**:
- ✅ Test avec **nouveau cursor** (méthode corrigée)
- ✅ 3 insertions successives pour tester la répétabilité
- ✅ Vérification de l'intégrité des données
- ✅ Test décodage PIL
- ✅ Test avec **cursor réutilisé** (ancienne méthode) pour comparaison

**Exécution**:
```bash
cd pythonRAs
python test_simple_insertion.py
```

**Sortie attendue**:
```
TEST 1: Méthode corrigée
  ✅ Insertion 1 réussie
  ✅ Insertion 2 réussie
  ✅ Insertion 3 réussie
  ✅ Données identiques
  ✅ Décodage PIL réussi

TEST 2: Ancienne méthode
  ✅ ou ❌ (selon le problème)
```

---

### 3. `test_capture_fix.py` - Test Unitaire

**Objectif**: Simuler `capture_photos_continu.py` avec une seule photo

**Ce qu'il fait**:
- ✅ Crée une photo de test
- ✅ Insère avec la méthode corrigée
- ✅ Vérifie dans la BD
- ✅ Crée un événement

**Exécution**:
```bash
cd pythonRAs
python test_capture_fix.py
```

---

## 🎯 Plan de Debugging

### Étape 1: Analyser les Photos Existantes

```bash
python debug_photo_blob.py
```

**Questions à répondre**:
- [ ] Y a-t-il des photos dans la BD?
- [ ] Quelle est leur taille?
- [ ] Sont-elles décodables avec PIL?
- [ ] Les magic bytes sont-ils corrects?
- [ ] Le cycle insert → select → decode fonctionne-t-il?

**Si ça échoue**: Le problème est dans l'encodage/décodage de base

---

### Étape 2: Test Simple d'Insertion

```bash
python test_simple_insertion.py
```

**Questions à répondre**:
- [ ] La méthode corrigée (nouveau cursor) fonctionne-t-elle?
- [ ] Peut-on faire 3 insertions successives?
- [ ] Les données sont-elles identiques après récupération?
- [ ] L'ancienne méthode échoue-t-elle?

**Si la méthode corrigée échoue**: Le problème n'est PAS le cursor

---

### Étape 3: Test de `capture_photos_continu.py`

```bash
sudo python capture_photos_continu.py
```

**Observations**:
- [ ] Photo #1 réussit?
- [ ] Photo #2 réussit?
- [ ] Photo #3 réussit?
- [ ] Quel message d'erreur exact?

---

## 🔍 Checklist de Diagnostic

### A. Problème de Connexion BD?

```bash
python -c "from db_connection import DatabaseConnection; \
           from config import *; \
           db = DatabaseConnection(DB_SERVER, DB_NAME, DB_USERNAME, DB_PASSWORD); \
           print('OK' if db.connect() else 'FAIL')"
```

### B. Problème de Capteur?

```sql
SELECT * FROM Capteur WHERE type = 'CAMERA';
```

Doit retourner au moins 1 ligne.

### C. Problème de Salle?

```sql
SELECT * FROM Salle WHERE idSalle_PK = 1;
```

Doit exister.

### D. Structure de la Table?

```sql
EXEC sp_help 'Donnees';
```

Vérifier:
- `photoBlob` est bien `VARBINARY(MAX)`
- `idCapteur` et `noSalle` sont bien des `INT`

### E. Permissions?

```sql
-- Tester l'insertion manuelle
INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
VALUES (GETDATE(), 1, NULL, 0x89504E470D0A1A0A, 1);
```

---

## 🐛 Erreurs Possibles et Solutions

### Erreur 1: "Cannot insert NULL into column 'noSalle'"

**Cause**: `noSalle` est NOT NULL mais le paramètre n'est pas passé

**Solution**: Vérifier que `ID_SALLE` dans `config.py` existe

```python
# config.py
ID_SALLE = 1  # Doit exister dans la table Salle
```

---

### Erreur 2: "Invalid cursor state"

**Cause**: Réutilisation du cursor

**Solution**: Utiliser `cursor = db.connection.cursor()` au lieu de `cursor = db.cursor`

---

### Erreur 3: "String or binary data would be truncated"

**Cause**: Photo trop grande pour la colonne

**Solution**: Vérifier que `photoBlob` est `VARBINARY(MAX)` et non `VARBINARY(n)`

```sql
ALTER TABLE Donnees ALTER COLUMN photoBlob VARBINARY(MAX);
```

---

### Erreur 4: "The driver reported that it has pending results"

**Cause**: Résultats non consommés du cursor précédent

**Solution**: Fermer le cursor avec `cursor.close()` ou utiliser un nouveau cursor

---

### Erreur 5: Photo insérée mais décodage échoue

**Cause**: Données corrompues pendant l'insertion

**Solution**: Utiliser `debug_photo_blob.py` pour comparer les bytes avant/après

---

## 📊 Matrice de Diagnostic

| Symptôme | Cause Probable | Test à Faire |
|----------|----------------|--------------|
| Photo #1 OK, #2+ échoue | Cursor réutilisé | `test_simple_insertion.py` |
| Aucune photo ne s'insère | Config/Permissions | Vérifier connexion BD |
| Photo insérée mais corrompue | Encodage incorrect | `debug_photo_blob.py` |
| "Invalid cursor state" | Pas de cursor.close() | Vérifier le code |
| Photo NULL dans BD | Paramètres inversés | Vérifier l'ordre des params |
| Timeout/Deadlock | Transactions non fermées | Ajouter commit/rollback |

---

## 📝 Rapporter un Bug

Si les tests échouent, fournir:

1. **Sortie de `debug_photo_blob.py`**
2. **Sortie de `test_simple_insertion.py`**
3. **Message d'erreur complet avec traceback**
4. **Version de pyodbc**: `python -c "import pyodbc; print(pyodbc.version)"`
5. **Version de Python**: `python --version`
6. **Version de SQL Server**: `SELECT @@VERSION`

---

## ✅ Résultats Attendus (si tout va bien)

### `debug_photo_blob.py`
```
✓ 10 photos trouvées
✓ Toutes décodables
✓ Format: JPEG/PNG
✓ Magic bytes corrects
✓ Test cycle: ✅ RÉUSSI
✓ Comparaison binaire: IDENTIQUE
```

### `test_simple_insertion.py`
```
✅ INSERTION 1: ID 123
✅ INSERTION 2: ID 124
✅ INSERTION 3: ID 125
✅ Données identiques
✅ Décodage PIL réussi
```

### `capture_photos_continu.py`
```
[12:34:56] Photo #1 envoyée (45.2 KB) - ID: 126
[12:35:01] Photo #2 envoyée (46.8 KB) - ID: 127
[12:35:06] Photo #3 envoyée (44.1 KB) - ID: 128
...
```

---

**Date**: 2025-11-27
**Scripts**: debug_photo_blob.py, test_simple_insertion.py, test_capture_fix.py
