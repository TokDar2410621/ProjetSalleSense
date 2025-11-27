"""
Script de debugging pour analyser les photos BLOB dans la BD
Teste la lecture, décodage et validation des photos
"""

import pyodbc
import os
from datetime import datetime
from PIL import Image
from io import BytesIO

# Configuration de connexion
SERVER = "DICJWIN01.cegepjonquiere.ca"
DATABASE = "Prog3A25_bdSalleSense"
USERNAME = "prog3e09"
PASSWORD = "colonne42"

# Dossier de sortie pour les tests
OUTPUT_DIR = "test_photos_debug"


def get_connection():
    """Établit une connexion avec authentification SQL"""
    drivers = [
        "ODBC Driver 18 for SQL Server",
        "ODBC Driver 17 for SQL Server",
        "ODBC Driver 13 for SQL Server",
        "SQL Server Native Client 11.0",
        "SQL Server"
    ]

    for driver in drivers:
        try:
            connection_string = (
                f"DRIVER={{{driver}}};"
                f"SERVER={SERVER};"
                f"DATABASE={DATABASE};"
                f"UID={USERNAME};"
                f"PWD={PASSWORD};"
                f"TrustServerCertificate=yes;"
            )
            conn = pyodbc.connect(connection_string)
            print(f"✓ Connexion réussie avec: {driver}\n")
            return conn
        except pyodbc.Error as e:
            continue

    print("✗ Impossible de se connecter")
    return None


def analyser_photos_bd(conn):
    """Analyse toutes les photos dans la BD"""
    print("=" * 80)
    print("  ANALYSE DES PHOTOS BLOB DANS LA BASE DE DONNÉES")
    print("=" * 80 + "\n")

    cursor = conn.cursor()

    # Récupérer les statistiques
    cursor.execute("""
        SELECT
            COUNT(*) as NbTotal,
            COUNT(photoBlob) as NbAvecPhoto,
            SUM(CASE WHEN photoBlob IS NULL THEN 1 ELSE 0 END) as NbNull,
            AVG(DATALENGTH(photoBlob)) as TailleMoyenne,
            MIN(DATALENGTH(photoBlob)) as TailleMin,
            MAX(DATALENGTH(photoBlob)) as TailleMax
        FROM Donnees
    """)

    stats = cursor.fetchone()
    print("📊 STATISTIQUES GLOBALES:")
    print(f"   Total d'enregistrements: {stats[0]}")
    print(f"   Avec photo BLOB: {stats[1]}")
    print(f"   Sans photo (NULL): {stats[2]}")
    if stats[3]:
        print(f"   Taille moyenne: {stats[3]/1024:.2f} KB")
        print(f"   Taille min: {stats[4]/1024:.2f} KB")
        print(f"   Taille max: {stats[5]/1024:.2f} KB")
    print()

    # Récupérer les 10 dernières photos
    cursor.execute("""
        SELECT TOP 10
            idDonnee_PK,
            dateHeure,
            idCapteur,
            noSalle,
            DATALENGTH(photoBlob) as TailleBLOB,
            photoBlob
        FROM Donnees
        WHERE photoBlob IS NOT NULL
        ORDER BY idDonnee_PK DESC
    """)

    photos = cursor.fetchall()

    if not photos:
        print("❌ Aucune photo trouvée dans la BD")
        return []

    print(f"📷 {len(photos)} PHOTOS TROUVÉES:\n")
    print("-" * 80)

    resultats = []

    for i, row in enumerate(photos, 1):
        id_donnee = row[0]
        date_heure = row[1]
        id_capteur = row[2]
        no_salle = row[3]
        taille_blob = row[4]
        photo_blob = row[5]

        print(f"\n[Photo #{i}]")
        print(f"  ID Donnée: {id_donnee}")
        print(f"  Date/Heure: {date_heure}")
        print(f"  Capteur: {id_capteur}")
        print(f"  Salle: {no_salle}")
        print(f"  Taille BLOB: {taille_blob} bytes ({taille_blob/1024:.2f} KB)")

        # Analyser les premiers bytes (magic bytes)
        if photo_blob and len(photo_blob) > 10:
            magic_bytes = photo_blob[:10]
            hex_display = ' '.join(f'{b:02X}' for b in magic_bytes)
            print(f"  Magic bytes: {hex_display}")

            # Identifier le format
            if photo_blob[0:2] == b'\xFF\xD8':
                format_detect = "JPEG"
            elif photo_blob[0:4] == b'\x89PNG':
                format_detect = "PNG"
            elif photo_blob[0:3] == b'GIF':
                format_detect = "GIF"
            elif photo_blob[0:2] == b'BM':
                format_detect = "BMP"
            else:
                format_detect = "INCONNU"

            print(f"  Format détecté: {format_detect}")

            # Essayer de décoder avec PIL
            try:
                img = Image.open(BytesIO(photo_blob))
                print(f"  ✓ DÉCODAGE PIL RÉUSSI")
                print(f"    Format PIL: {img.format}")
                print(f"    Taille: {img.size[0]}x{img.size[1]}")
                print(f"    Mode: {img.mode}")

                resultats.append({
                    'id': id_donnee,
                    'taille': taille_blob,
                    'format': format_detect,
                    'decodable': True,
                    'blob': photo_blob
                })

            except Exception as e:
                print(f"  ✗ ÉCHEC DÉCODAGE PIL: {e}")
                resultats.append({
                    'id': id_donnee,
                    'taille': taille_blob,
                    'format': format_detect,
                    'decodable': False,
                    'blob': photo_blob
                })
        else:
            print(f"  ⚠ BLOB trop petit ou vide")
            resultats.append({
                'id': id_donnee,
                'taille': taille_blob,
                'format': 'VIDE',
                'decodable': False,
                'blob': photo_blob
            })

    cursor.close()
    print("\n" + "-" * 80)
    return resultats


def sauvegarder_photos(resultats):
    """Sauvegarde les photos décodables sur disque"""
    if not resultats:
        return

    # Créer le dossier de sortie
    if not os.path.exists(OUTPUT_DIR):
        os.makedirs(OUTPUT_DIR)
        print(f"\n✓ Dossier créé: {OUTPUT_DIR}")

    print(f"\n📁 SAUVEGARDE DES PHOTOS DANS {OUTPUT_DIR}/\n")

    nb_sauvegardes = 0

    for photo in resultats:
        if not photo['decodable']:
            continue

        try:
            # Essayer de sauvegarder
            img = Image.open(BytesIO(photo['blob']))

            # Déterminer l'extension
            if img.format == 'JPEG':
                ext = 'jpg'
            elif img.format == 'PNG':
                ext = 'png'
            else:
                ext = img.format.lower()

            filename = f"photo_{photo['id']}.{ext}"
            filepath = os.path.join(OUTPUT_DIR, filename)

            img.save(filepath)
            print(f"  ✓ {filename} ({photo['taille']/1024:.2f} KB)")
            nb_sauvegardes += 1

        except Exception as e:
            print(f"  ✗ Échec sauvegarde photo {photo['id']}: {e}")

    print(f"\n✓ {nb_sauvegardes} photo(s) sauvegardée(s)")


def tester_insertion_recuperation(conn):
    """Teste un cycle complet: insertion → récupération → décodage"""
    print("\n" + "=" * 80)
    print("  TEST CYCLE COMPLET: INSERT → SELECT → DECODE")
    print("=" * 80 + "\n")

    cursor = conn.cursor()

    # Créer une image de test simple (carré rouge 100x100)
    print("1️⃣  Création d'une image de test...")
    img_test = Image.new('RGB', (100, 100), color='red')
    buffer = BytesIO()
    img_test.save(buffer, format='PNG')
    photo_test_bytes = buffer.getvalue()
    buffer.close()

    print(f"   ✓ Image créée: 100x100 PNG")
    print(f"   ✓ Taille: {len(photo_test_bytes)} bytes")
    print(f"   ✓ Magic bytes: {' '.join(f'{b:02X}' for b in photo_test_bytes[:4])}")

    # Récupérer un capteur CAMERA
    cursor.execute("SELECT TOP 1 idCapteur_PK FROM Capteur WHERE type = 'CAMERA'")
    capteur = cursor.fetchone()

    if not capteur:
        print("\n✗ Aucun capteur CAMERA trouvé")
        return False

    id_capteur = capteur[0]
    print(f"\n2️⃣  Capteur CAMERA trouvé: ID {id_capteur}")

    # Insertion
    print("\n3️⃣  Insertion dans la BD...")
    query_insert = """
        INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
        VALUES (GETDATE(), ?, NULL, ?, ?)
    """

    try:
        cursor.execute(query_insert, (id_capteur, photo_test_bytes, 1))
        conn.commit()

        cursor.execute("SELECT @@IDENTITY")
        id_insere = cursor.fetchone()[0]

        print(f"   ✓ Photo insérée avec ID: {id_insere}")

    except Exception as e:
        print(f"   ✗ Échec insertion: {e}")
        import traceback
        traceback.print_exc()
        return False

    # Récupération
    print("\n4️⃣  Récupération de la photo...")
    query_select = """
        SELECT photoBlob, DATALENGTH(photoBlob) as taille
        FROM Donnees
        WHERE idDonnee_PK = ?
    """

    cursor.execute(query_select, (int(id_insere),))
    row = cursor.fetchone()

    if not row or not row[0]:
        print("   ✗ Photo non trouvée ou NULL")
        return False

    photo_recuperee = row[0]
    taille_recuperee = row[1]

    print(f"   ✓ Photo récupérée: {taille_recuperee} bytes")
    print(f"   ✓ Magic bytes: {' '.join(f'{b:02X}' for b in photo_recuperee[:4])}")

    # Comparaison binaire
    print("\n5️⃣  Comparaison des données...")
    if photo_recuperee == photo_test_bytes:
        print("   ✅ IDENTIQUE: Les bytes sont exactement les mêmes!")
    else:
        print("   ❌ DIFFÉRENT: Les bytes ne correspondent pas!")
        print(f"      Taille originale: {len(photo_test_bytes)}")
        print(f"      Taille récupérée: {len(photo_recuperee)}")

        # Afficher les différences
        for i in range(min(20, len(photo_test_bytes), len(photo_recuperee))):
            if photo_test_bytes[i] != photo_recuperee[i]:
                print(f"      Différence à l'index {i}: {photo_test_bytes[i]:02X} != {photo_recuperee[i]:02X}")

    # Décodage
    print("\n6️⃣  Décodage de la photo récupérée...")
    try:
        img_recuperee = Image.open(BytesIO(photo_recuperee))
        print(f"   ✅ DÉCODAGE RÉUSSI!")
        print(f"      Format: {img_recuperee.format}")
        print(f"      Taille: {img_recuperee.size}")
        print(f"      Mode: {img_recuperee.mode}")

        # Sauvegarder pour vérification visuelle
        test_filename = os.path.join(OUTPUT_DIR, f"test_cycle_{id_insere}.png")
        img_recuperee.save(test_filename)
        print(f"   ✓ Sauvegardé: {test_filename}")

        return True

    except Exception as e:
        print(f"   ❌ ÉCHEC DÉCODAGE: {e}")
        import traceback
        traceback.print_exc()
        return False

    finally:
        cursor.close()


def main():
    """Fonction principale"""
    print("\n╔" + "═" * 78 + "╗")
    print("║" + " " * 20 + "DEBUG PHOTO BLOB - ANALYSE COMPLÈTE" + " " * 23 + "║")
    print("╚" + "═" * 78 + "╝\n")

    # Connexion
    conn = get_connection()
    if not conn:
        return

    try:
        # Analyse des photos existantes
        resultats = analyser_photos_bd(conn)

        # Sauvegarder les photos décodables
        if resultats:
            sauvegarder_photos(resultats)

        # Test cycle complet
        success = tester_insertion_recuperation(conn)

        # Résumé
        print("\n" + "=" * 80)
        print("  RÉSUMÉ")
        print("=" * 80)
        print(f"  Photos analysées: {len(resultats)}")
        print(f"  Photos décodables: {sum(1 for r in resultats if r['decodable'])}")
        print(f"  Test cycle complet: {'✅ RÉUSSI' if success else '❌ ÉCHOUÉ'}")
        print("=" * 80 + "\n")

    finally:
        conn.close()
        print("✓ Connexion fermée\n")


if __name__ == "__main__":
    main()
