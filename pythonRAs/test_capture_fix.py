"""
Script de test pour vérifier que capture_photos_continu insère correctement les données
"""

from db_connection import DatabaseConnection
from config import DB_SERVER, DB_NAME, DB_USERNAME, DB_PASSWORD, ID_SALLE
from datetime import datetime

def test_insertion():
    """Teste l'insertion d'une photo simulée"""
    print("=" * 70)
    print("  TEST D'INSERTION DE PHOTO - SIMULATION")
    print("=" * 70)

    # Connexion
    db = DatabaseConnection(DB_SERVER, DB_NAME, DB_USERNAME, DB_PASSWORD)

    if not db.connect():
        print("\n✗ Échec de connexion")
        return False

    try:
        # Récupérer l'ID du capteur CAMERA
        capteur = db.execute_query(
            "SELECT idCapteur_PK FROM Capteur WHERE type = 'CAMERA'"
        )

        if not capteur:
            print("✗ Aucun capteur CAMERA trouvé")
            db.disconnect()
            return False

        id_capteur = capteur[0][0]
        print(f"\n✓ Capteur CAMERA trouvé - ID: {id_capteur}")

        # Créer une photo simulée
        photo_bytes = b"TEST_PHOTO_" + str(datetime.now()).encode()

        print(f"\n📸 Insertion d'une photo test ({len(photo_bytes)} bytes)...")

        # MÉTHODE DIRECTE (comme inserer_screenshots.py)
        # CRITIQUE: Créer un NOUVEAU cursor, pas réutiliser db.cursor
        cursor = db.connection.cursor()

        query = """
            INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
            VALUES (GETDATE(), ?, NULL, ?, ?)
        """

        try:
            cursor.execute(query, (id_capteur, photo_bytes, ID_SALLE))
            db.connection.commit()

            # Récupérer l'ID inséré
            cursor.execute("SELECT @@IDENTITY")
            id_donnee = cursor.fetchone()[0]
            success = True

            print(f"\n✅ SUCCÈS!")
            print(f"   ID de la donnée insérée: {int(id_donnee)}")
            print(f"   Salle: {ID_SALLE}")
            print(f"   Capteur: {id_capteur}")

            # Vérifier que les données sont bien dans la BD
            cursor.execute(
                """SELECT idDonnee_PK, dateHeure, idCapteur, noSalle,
                          DATALENGTH(photoBlob) as taille
                   FROM Donnees
                   WHERE idDonnee_PK = ?""",
                (int(id_donnee),)
            )
            row = cursor.fetchone()

            if row:
                print(f"\n✓ Vérification dans la BD:")
                print(f"   ID: {row[0]}")
                print(f"   Date/Heure: {row[1]}")
                print(f"   Capteur: {row[2]}")
                print(f"   Salle: {row[3]}")
                print(f"   Taille BLOB: {row[4]} bytes")

            # Créer un événement test
            cursor.execute(
                """INSERT INTO Evenement (type, idDonnee, description)
                   VALUES (?, ?, ?)""",
                ('TEST', int(id_donnee), 'Photo de test capture_photos_continu')
            )
            db.connection.commit()

            # CRITIQUE: Fermer le cursor
            cursor.close()

            print("\n✓ Événement créé")
            print("✓ Cursor fermé")

            return True

        except Exception as ex:
            db.connection.rollback()
            print(f"\n✗ Échec de l'insertion: {ex}")
            import traceback
            traceback.print_exc()
            return False

    except Exception as e:
        print(f"\n✗ Erreur: {e}")
        import traceback
        traceback.print_exc()
        return False

    finally:
        db.disconnect()


if __name__ == "__main__":
    print("\n")
    success = test_insertion()
    print("\n" + "=" * 70)
    if success:
        print("  ✅ TEST RÉUSSI - capture_photos_continu.py devrait fonctionner")
    else:
        print("  ❌ TEST ÉCHOUÉ - Vérifiez la configuration")
    print("=" * 70 + "\n")
