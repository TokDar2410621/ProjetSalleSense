"""
Test simple d'insertion identique à capture_photos_continu.py
Pour isoler le problème
"""

from db_connection import DatabaseConnection
from config import DB_SERVER, DB_NAME, DB_USERNAME, DB_PASSWORD, ID_SALLE
from datetime import datetime
from PIL import Image
from io import BytesIO

def test_insertion_simple():
    """Test d'insertion avec la même logique que capture_photos_continu"""
    print("=" * 70)
    print("  TEST SIMPLE D'INSERTION (même logique que capture_photos_continu)")
    print("=" * 70 + "\n")

    # Connexion (comme capture_photos_continu.py)
    db = DatabaseConnection(DB_SERVER, DB_NAME, DB_USERNAME, DB_PASSWORD)

    if not db.connect():
        print("✗ Échec de connexion")
        return False

    # Créer une image de test
    print("1️⃣  Création d'une image de test (carré bleu 200x200)...")
    img = Image.new('RGB', (200, 200), color='blue')
    buffer = BytesIO()
    img.save(buffer, format='JPEG', quality=85)
    photo_bytes = buffer.getvalue()
    buffer.close()

    print(f"   ✓ Image créée: {len(photo_bytes)} bytes")
    print(f"   ✓ Format: JPEG")
    print(f"   ✓ Magic bytes: {' '.join(f'{b:02X}' for b in photo_bytes[:4])}")

    # Récupérer le capteur (comme capture_photos_continu.py ligne 45-46)
    print("\n2️⃣  Récupération du capteur CAMERA...")
    try:
        capteur = db.execute_query(
            "SELECT idCapteur_PK FROM Capteur WHERE type = 'CAMERA'"
        )

        if not capteur:
            print("   ✗ Aucun capteur CAMERA")
            db.disconnect()
            return False

        id_capteur_camera = capteur[0][0]
        print(f"   ✓ Capteur trouvé: ID {id_capteur_camera}")

    except Exception as e:
        print(f"   ✗ Erreur: {e}")
        db.disconnect()
        return False

    # TEST 1: Méthode avec nouveau cursor (CORRIGÉE)
    print("\n3️⃣  TEST INSERTION (méthode corrigée)...")
    print("   Méthode: cursor = db.connection.cursor()")

    try:
        date_heure = datetime.now()

        # CRÉER un nouveau cursor (ligne 129)
        cursor = db.connection.cursor()

        query = """
            INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
            VALUES (GETDATE(), ?, NULL, ?, ?)
        """

        print(f"   Paramètres:")
        print(f"     - idCapteur: {id_capteur_camera} (type: {type(id_capteur_camera)})")
        print(f"     - photoBlob: {len(photo_bytes)} bytes (type: {type(photo_bytes)})")
        print(f"     - noSalle: {ID_SALLE} (type: {type(ID_SALLE)})")

        cursor.execute(query, (id_capteur_camera, photo_bytes, ID_SALLE))
        db.connection.commit()

        cursor.execute("SELECT @@IDENTITY")
        id_donnee = cursor.fetchone()[0]

        print(f"\n   ✅ INSERTION RÉUSSIE!")
        print(f"      ID: {id_donnee}")

        # Vérifier la récupération
        cursor.execute("""
            SELECT photoBlob, DATALENGTH(photoBlob) as taille
            FROM Donnees
            WHERE idDonnee_PK = ?
        """, (int(id_donnee),))

        row = cursor.fetchone()
        photo_recuperee = row[0]
        taille_recuperee = row[1]

        print(f"      Taille récupérée: {taille_recuperee} bytes")

        # Comparer
        if photo_recuperee == photo_bytes:
            print(f"      ✅ DONNÉES IDENTIQUES")
        else:
            print(f"      ❌ DONNÉES DIFFÉRENTES!")
            print(f"         Originale: {len(photo_bytes)} bytes")
            print(f"         Récupérée: {len(photo_recuperee)} bytes")

        # Tester le décodage
        try:
            img_test = Image.open(BytesIO(photo_recuperee))
            print(f"      ✅ DÉCODAGE PIL RÉUSSI")
            print(f"         Format: {img_test.format}")
            print(f"         Taille: {img_test.size}")
        except Exception as e:
            print(f"      ❌ ÉCHEC DÉCODAGE: {e}")

        cursor.close()

        # TEST 2: Deuxième insertion pour tester la répétabilité
        print("\n4️⃣  TEST DEUXIÈME INSERTION (vérifier répétabilité)...")

        cursor2 = db.connection.cursor()

        cursor2.execute(query, (id_capteur_camera, photo_bytes, ID_SALLE))
        db.connection.commit()

        cursor2.execute("SELECT @@IDENTITY")
        id_donnee2 = cursor2.fetchone()[0]

        print(f"   ✅ DEUXIÈME INSERTION RÉUSSIE!")
        print(f"      ID: {id_donnee2}")

        cursor2.close()

        # TEST 3: Troisième insertion
        print("\n5️⃣  TEST TROISIÈME INSERTION...")

        cursor3 = db.connection.cursor()

        cursor3.execute(query, (id_capteur_camera, photo_bytes, ID_SALLE))
        db.connection.commit()

        cursor3.execute("SELECT @@IDENTITY")
        id_donnee3 = cursor3.fetchone()[0]

        print(f"   ✅ TROISIÈME INSERTION RÉUSSIE!")
        print(f"      ID: {id_donnee3}")

        cursor3.close()

        print("\n✅ TOUS LES TESTS RÉUSSIS!")
        print(f"   IDs insérés: {id_donnee}, {id_donnee2}, {id_donnee3}")

        return True

    except Exception as e:
        print(f"\n❌ ERREUR: {e}")
        import traceback
        traceback.print_exc()
        db.connection.rollback()
        return False

    finally:
        db.disconnect()


def test_avec_ancien_cursor():
    """Test avec l'ancienne méthode (réutilisation du cursor)"""
    print("\n" + "=" * 70)
    print("  TEST AVEC ANCIENNE MÉTHODE (réutilisation cursor)")
    print("=" * 70 + "\n")

    db = DatabaseConnection(DB_SERVER, DB_NAME, DB_USERNAME, DB_PASSWORD)

    if not db.connect():
        print("✗ Échec de connexion")
        return False

    # Créer une image
    img = Image.new('RGB', (50, 50), color='green')
    buffer = BytesIO()
    img.save(buffer, format='PNG')
    photo_bytes = buffer.getvalue()
    buffer.close()

    # Récupérer le capteur
    capteur = db.execute_query("SELECT idCapteur_PK FROM Capteur WHERE type = 'CAMERA'")
    if not capteur:
        db.disconnect()
        return False

    id_capteur = capteur[0][0]

    query = """
        INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
        VALUES (GETDATE(), ?, NULL, ?, ?)
    """

    try:
        print("Tentative 1 (cursor réutilisé)...")
        # ANCIENNE MÉTHODE: réutiliser self.db.cursor
        cursor = db.cursor

        cursor.execute(query, (id_capteur, photo_bytes, ID_SALLE))
        db.connection.commit()
        cursor.execute("SELECT @@IDENTITY")
        id1 = cursor.fetchone()[0]
        print(f"   ✓ ID: {id1}")

        print("\nTentative 2 (même cursor)...")
        cursor.execute(query, (id_capteur, photo_bytes, ID_SALLE))
        db.connection.commit()
        cursor.execute("SELECT @@IDENTITY")
        id2 = cursor.fetchone()[0]
        print(f"   ✓ ID: {id2}")

        print("\nTentative 3 (même cursor)...")
        cursor.execute(query, (id_capteur, photo_bytes, ID_SALLE))
        db.connection.commit()
        cursor.execute("SELECT @@IDENTITY")
        id3 = cursor.fetchone()[0]
        print(f"   ✓ ID: {id3}")

        print("\n⚠️  Si ça fonctionne ici, le problème est ailleurs!")
        return True

    except Exception as e:
        print(f"\n❌ ERREUR (comme prévu): {e}")
        import traceback
        traceback.print_exc()
        return False

    finally:
        db.disconnect()


if __name__ == "__main__":
    print("\n")

    # Test avec la méthode corrigée
    success1 = test_insertion_simple()

    # Test avec l'ancienne méthode
    success2 = test_avec_ancien_cursor()

    print("\n" + "=" * 70)
    print("  RÉSUMÉ DES TESTS")
    print("=" * 70)
    print(f"  Méthode corrigée (nouveau cursor): {'✅ RÉUSSI' if success1 else '❌ ÉCHOUÉ'}")
    print(f"  Ancienne méthode (cursor réutilisé): {'✅ RÉUSSI' if success2 else '❌ ÉCHOUÉ'}")
    print("=" * 70 + "\n")
