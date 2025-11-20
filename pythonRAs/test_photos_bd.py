"""
Script de diagnostic pour vérifier les photos dans la BD
"""

import pyodbc
import os

# Configuration de connexion
SERVER = "(localdb)\\MSSQLLocalDB"
DATABASE = "Prog3A25_bdSalleSense"


def get_connection():
    """Établit une connexion avec Windows Authentication"""
    drivers = [
        "ODBC Driver 17 for SQL Server",
        "ODBC Driver 18 for SQL Server",
        "ODBC Driver 13 for SQL Server",
    ]

    for driver in drivers:
        try:
            connection_string = (
                f"DRIVER={{{driver}}};"
                f"SERVER={SERVER};"
                f"DATABASE={DATABASE};"
                f"Integrated Security=true;"
                f"TrustServerCertificate=yes;"
            )
            return pyodbc.connect(connection_string)
        except:
            continue
    return None


def diagnostiquer_photos():
    """Diagnostic complet des photos dans la BD"""

    print("\n" + "="*70)
    print("  DIAGNOSTIC DES PHOTOS - SalleSense")
    print("="*70 + "\n")

    conn = get_connection()
    if not conn:
        print("✗ Impossible de se connecter à la base de données")
        return

    try:
        cursor = conn.cursor()

        # 1. Vérifier le nombre total de données
        print("1. Nombre total de lignes dans Donnees:")
        cursor.execute("SELECT COUNT(*) FROM Donnees")
        total = cursor.fetchone()[0]
        print(f"   Total: {total} ligne(s)\n")

        # 2. Vérifier les données de type CAMERA
        print("2. Données du capteur CAMERA:")
        cursor.execute("""
            SELECT COUNT(*)
            FROM Donnees d
            JOIN Capteur c ON d.idCapteur = c.idCapteur_PK
            WHERE c.type = 'CAMERA'
        """)
        camera_data = cursor.fetchone()[0]
        print(f"   Total lignes CAMERA: {camera_data}\n")

        # 3. Vérifier les photoBlob NON NULL
        print("3. Photos avec photoBlob non NULL:")
        cursor.execute("""
            SELECT COUNT(*)
            FROM Donnees
            WHERE photoBlob IS NOT NULL
        """)
        photos_non_null = cursor.fetchone()[0]
        print(f"   Total: {photos_non_null} photo(s)\n")

        # 4. Vérifier la taille des photoBlob
        print("4. Taille des photoBlob:")
        cursor.execute("""
            SELECT
                idDonnee_PK,
                dateHeure,
                DATALENGTH(photoBlob) AS taille_bytes
            FROM Donnees
            WHERE photoBlob IS NOT NULL
            ORDER BY dateHeure DESC
        """)
        tailles = cursor.fetchall()

        if tailles:
            print(f"   Nombre de photos: {len(tailles)}")
            print("\n   Détails:")
            print("   " + "-"*60)
            for row in tailles[:10]:  # Afficher les 10 dernières
                id_donnee = row[0]
                date_heure = row[1]
                taille_bytes = row[2]
                taille_kb = taille_bytes / 1024

                # Analyser la taille
                statut = "OK" if taille_bytes > 1000 else "SUSPECT (trop petit)"

                print(f"   ID: {id_donnee:4d} | {date_heure} | {taille_kb:8.1f} KB | {statut}")

            if len(tailles) > 10:
                print(f"   ... et {len(tailles) - 10} autre(s)")
        else:
            print("   ✗ Aucune photo trouvée!")

        print("\n" + "="*70)

        # 5. Tester l'extraction d'une photo
        if tailles:
            print("\n5. Test d'extraction de la dernière photo:")
            id_test = tailles[0][0]

            cursor.execute(
                "SELECT photoBlob FROM Donnees WHERE idDonnee_PK = ?",
                (id_test,)
            )
            result = cursor.fetchone()

            if result and result[0]:
                photo_bytes = result[0]
                print(f"   ✓ Photo ID {id_test} récupérée")
                print(f"   Taille: {len(photo_bytes)} bytes")
                print(f"   Type Python: {type(photo_bytes)}")

                # Vérifier si c'est du JPEG ou PNG
                if photo_bytes[:2] == b'\xff\xd8':
                    print("   ✓ Signature JPEG valide (FF D8)")
                    extension = "jpg"
                elif photo_bytes[:8] == b'\x89PNG\r\n\x1a\n':
                    print("   ✓ Signature PNG valide (89 50 4E 47)")
                    extension = "png"
                else:
                    premiers_bytes = photo_bytes[:20]
                    print(f"   ⚠ Format inconnu!")
                    print(f"   Premiers bytes: {premiers_bytes.hex()}")
                    extension = "bin"

                # Essayer de sauvegarder
                os.makedirs("test_extraction", exist_ok=True)
                test_file = f"test_extraction/test_photo_{id_test}.{extension}"

                with open(test_file, 'wb') as f:
                    f.write(photo_bytes)

                print(f"   ✓ Photo test sauvegardée: {test_file}")
                print(f"   → Essayez d'ouvrir ce fichier pour vérifier!")

            else:
                print("   ✗ Impossible de récupérer la photo")

    except Exception as e:
        print(f"\n✗ ERREUR: {e}")
        import traceback
        traceback.print_exc()

    finally:
        conn.close()
        print("\n" + "="*70 + "\n")


if __name__ == "__main__":
    diagnostiquer_photos()