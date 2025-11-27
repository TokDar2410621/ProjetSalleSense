# 🔍 Analyse Détaillée: inserer_screenshots.py vs capture_photos_continu.py

## Différences Critiques Identifiées

### 1. **CURSOR CREATION** ⚠️ DIFFÉRENCE MAJEURE

#### ✅ inserer_screenshots.py (FONCTIONNE)
```python
def inserer_photo_blob(conn, photo_path, id_capteur, no_salle=1):
    # conn est une connexion pyodbc DIRECTE
    cursor = conn.cursor()  # ← CRÉE UN NOUVEAU CURSOR

    query = """..."""
    cursor.execute(query, (id_capteur, photo_blob, no_salle))
    conn.commit()
    cursor.execute("SELECT @@IDENTITY")
    id_donnees = cursor.fetchone()[0]
    cursor.close()  # ← FERME LE CURSOR
```

**Type**: `conn` est un objet `pyodbc.Connection`
**Cursor**: Nouveau cursor créé avec `conn.cursor()`

#### ❌ capture_photos_continu.py (PROBLÉMATIQUE)
```python
def envoyer_photo_bd(self, photo_bytes: bytes) -> bool:
    # self.db est une instance de DatabaseConnection
    cursor = self.db.cursor  # ← RÉUTILISE LE CURSOR EXISTANT

    query = """..."""
    cursor.execute(query, (self.id_capteur_camera, photo_bytes, self.id_salle))
    self.db.connection.commit()
    cursor.execute("SELECT @@IDENTITY")
    id_donnee = cursor.fetchone()[0]
    # PAS DE cursor.close()
```

**Type**: `self.db` est un objet `DatabaseConnection` (wrapper)
**Cursor**: Réutilise `self.cursor` créé dans `__init__`

---

## 🚨 LE PROBLÈME: Réutilisation du Cursor

### Dans DatabaseConnection (db_connection.py)

```python
class DatabaseConnection:
    def __init__(self, server, database, username=None, password=None):
        self.connection = None
        self.cursor = None  # ← Cursor partagé

    def connect(self):
        self.connection = pyodbc.connect(connection_string)
        self.cursor = self.connection.cursor()  # ← UN SEUL cursor pour toute la vie de l'objet
        return True
```

**Conséquence**: Le même cursor est réutilisé pour TOUTES les requêtes.

### Pourquoi C'est Problématique?

1. **État du cursor**: Après `cursor.fetchone()`, le cursor peut être dans un état "pending results"
2. **Transactions multiples**: Si une requête échoue, le cursor peut être corrompu
3. **Memory leaks**: Les résultats précédents peuvent rester en mémoire
4. **Timing issues**: Entre deux insertions, l'état peut être incohérent

---

## 📊 Comparaison Ligne par Ligne

| Aspect | inserer_screenshots.py | capture_photos_continu.py | Impact |
|--------|------------------------|---------------------------|--------|
| **Cursor creation** | `conn.cursor()` (nouveau) | `self.db.cursor` (réutilisé) | ⚠️ CRITIQUE |
| **Cursor close** | `cursor.close()` | ❌ Jamais fermé | ⚠️ CRITIQUE |
| **Connection type** | `pyodbc.Connection` directe | `DatabaseConnection` wrapper | Moyen |
| **Commit** | `conn.commit()` | `self.db.connection.commit()` | OK |
| **@@IDENTITY** | `SELECT @@IDENTITY` | `SELECT @@IDENTITY` | OK |
| **Error handling** | `pyodbc.Error` spécifique | `Exception` générique | Mineur |

---

## 🔧 Analyse du Flux

### Scénario: Plusieurs photos en continu

#### Photo 1:
```
cursor (état initial: propre)
  → execute INSERT
  → fetchone() pour @@IDENTITY
  → cursor (état: résultats consommés mais pas fermé)
```

#### Photo 2:
```
cursor (état: résultats précédents encore en mémoire?)
  → execute INSERT ← PEUT ÉCHOUER si cursor pas "clean"
  → fetchone() ← PEUT retourner le mauvais ID
```

---

## 🎯 Pourquoi "Ça Ne Marche Pas Toujours"

### Symptômes Possibles:

1. **Première photo OK, deuxième échoue**
   - Le cursor n'est pas réinitialisé entre deux insertions

2. **IDs incorrects**
   - `@@IDENTITY` peut retourner l'ID d'une transaction précédente

3. **Erreur intermittente "Invalid cursor state"**
   - pyodbc se plaint que le cursor a des résultats non consommés

4. **Timeout ou deadlock**
   - Les transactions s'empilent sans être correctement fermées

---

## ✅ Solution: Créer un Nouveau Cursor à Chaque Fois

### Option 1: Créer un nouveau cursor (RECOMMANDÉ)

```python
def envoyer_photo_bd(self, photo_bytes: bytes) -> bool:
    try:
        date_heure = datetime.now()

        # CRÉER un nouveau cursor à chaque appel
        cursor = self.db.connection.cursor()  # ← Nouveau cursor

        query = """
            INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
            VALUES (GETDATE(), ?, NULL, ?, ?)
        """

        cursor.execute(query, (self.id_capteur_camera, photo_bytes, self.id_salle))
        self.db.connection.commit()

        cursor.execute("SELECT @@IDENTITY")
        id_donnee = cursor.fetchone()[0]

        cursor.execute(
            """INSERT INTO Evenement (type, idDonnee, description)
               VALUES (?, ?, ?)""",
            ('CAPTURE', int(id_donnee), f'Photo capturée à {date_heure.strftime("%H:%M:%S")}')
        )
        self.db.connection.commit()

        cursor.close()  # ← FERMER le cursor

        self.compteur_photos += 1
        print(f"[{date_heure.strftime('%H:%M:%S')}] Photo #{self.compteur_photos} envoyée")

        return True

    except Exception as e:
        print(f"✗ Erreur: {e}")
        import traceback
        traceback.print_exc()
        self.db.connection.rollback()
        return False
```

### Option 2: Context Manager (MIEUX)

```python
def envoyer_photo_bd(self, photo_bytes: bytes) -> bool:
    try:
        date_heure = datetime.now()

        # Context manager nettoie automatiquement
        with self.db.connection.cursor() as cursor:
            query = """
                INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
                VALUES (GETDATE(), ?, NULL, ?, ?)
            """

            cursor.execute(query, (self.id_capteur_camera, photo_bytes, self.id_salle))
            self.db.connection.commit()

            cursor.execute("SELECT @@IDENTITY")
            id_donnee = cursor.fetchone()[0]

            cursor.execute(
                """INSERT INTO Evenement (type, idDonnee, description)
                   VALUES (?, ?, ?)""",
                ('CAPTURE', int(id_donnee), f'Photo capturée à {date_heure.strftime("%H:%M:%S")}')
            )
            self.db.connection.commit()

            self.compteur_photos += 1
            print(f"[{date_heure.strftime('%H:%M:%S')}] Photo #{self.compteur_photos} envoyée")

            return True

    except Exception as e:
        print(f"✗ Erreur: {e}")
        import traceback
        traceback.print_exc()
        self.db.connection.rollback()
        return False
```

---

## 📋 Checklist de Vérification

- [ ] Nouveau cursor créé avec `connection.cursor()`
- [ ] Cursor fermé avec `cursor.close()` ou context manager
- [ ] Commit après chaque transaction
- [ ] Rollback en cas d'erreur
- [ ] Traceback pour debugging
- [ ] Pas de réutilisation du cursor entre appels

---

## 🧪 Test pour Confirmer le Problème

```python
# Script de test pour reproduire le problème
from db_connection import DatabaseConnection
from config import DB_SERVER, DB_NAME, DB_USERNAME, DB_PASSWORD

db = DatabaseConnection(DB_SERVER, DB_NAME, DB_USERNAME, DB_PASSWORD)
db.connect()

# Simuler 10 insertions rapides
for i in range(10):
    photo_bytes = f"PHOTO_{i}".encode()

    # MÉTHODE BUGGUÉE (réutilise self.db.cursor)
    cursor = db.cursor
    cursor.execute("INSERT INTO Donnees (...) VALUES (...)", params)
    db.connection.commit()
    cursor.execute("SELECT @@IDENTITY")
    id_donnee = cursor.fetchone()[0]
    print(f"Photo {i}: ID {id_donnee}")

    # Pas de cursor.close() !
    # À partir de la 2ème itération, risque d'erreur

db.disconnect()
```

**Résultat attendu**: Échec après 1-2 insertions

---

## 💡 Conclusion

Le problème n'est PAS la conversion des bytes ni le nombre de paramètres.

**Le vrai problème**: **Réutilisation d'un cursor partagé sans le fermer entre les appels**

**La vraie solution**: **Créer un NOUVEAU cursor pour chaque insertion**

---

Date: 2025-11-27
Analysé par: Claude Code
