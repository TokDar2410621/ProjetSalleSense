"""
Script pour insérer des screenshots dans la base de données
Lit les images de C:\\Users\\Darius\\Pictures\\Screenshots et les insère en BLOB
"""

import pyodbc
import os
from glob import glob
from datetime import datetime

# Configuration de connexion (correspond à appsettings.Home.json)
SERVER = "(localdb)\\MSSQLLocalDB"
DATABASE = "Prog3A25_bdSalleSense"

# Dossier contenant les screenshots
SCREENSHOTS_DIR = r"C:\Users\Darius\Pictures\Screenshots"

# Nombre de screenshots à insérer par défaut
NB_SCREENSHOTS_PAR_DEFAUT = 10


def get_connection():
    """Établit une connexion avec Windows Authentication"""
    # Liste des drivers à essayer dans l'ordre
    drivers = [
        "ODBC Driver 17 for SQL Server",
        "ODBC Driver 18 for SQL Server",
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
                f"Integrated Security=true;"
                f"TrustServerCertificate=yes;"
            )
            conn = pyodbc.connect(connection_string)
            print(f"✓ Connexion réussie avec le driver: {driver}")
            return conn
        except pyodbc.Error as e:
            print(f"✗ Échec avec {driver}: {str(e)[:100]}")
            continue

    print("\n✗ Impossible de se connecter avec aucun driver disponible")
    print("\nVérifiez:")
    print(f"  - Serveur: {SERVER}")
    print(f"  - Base de données: {DATABASE}")
    print("  - LocalDB est démarré: sqllocaldb start MSSQLLocalDB")
    print("\nDrivers installés sur votre système:")
    for driver in pyodbc.drivers():
        print(f"  - {driver}")
    return None


def lister_capteurs_camera(conn):
    """Liste tous les capteurs de type CAMERA"""
    try:
        cursor = conn.cursor()

        # Essayer différentes variantes de noms de colonnes
        queries = [
            # Variante 1: Noms originaux
            "SELECT idCapteur_PK, idSalle_FK, type FROM Capteur WHERE type = 'CAMERA'",
            # Variante 2: Sans FK
            "SELECT idCapteur_PK, idSalle, type FROM Capteur WHERE type = 'CAMERA'",
            # Variante 3: Juste pour voir si la table existe
            "SELECT TOP 1 * FROM Capteur WHERE type = 'CAMERA'"
        ]

        rows = None
        for query in queries:
            try:
                cursor.execute(query)
                rows = cursor.fetchall()
                if rows:
                    print("\n=== Capteurs CAMERA disponibles ===")
                    for row in rows:
                        print(f"  ID Capteur: {row[0]}")
                        print(f"  Colonnes: {cursor.description}")
                    cursor.close()
                    return rows
            except pyodbc.Error:
                continue

        # Si aucune requête n'a fonctionné, afficher la structure
        print("\n=== Impossible de lire les capteurs ===")
        print("Structure de la table Capteur:")
        cursor.execute("SELECT TOP 1 * FROM Capteur")
        for col in cursor.description:
            print(f"  - {col[0]} ({col[1]})")

        cursor.close()
        return []

    except pyodbc.Error as e:
        print(f"✗ Erreur: {e}")
        return []


def inserer_photo_blob(conn, photo_path, id_capteur, no_salle=1):
    """Insère une photo en BLOB dans la table Donnees"""
    if not os.path.exists(photo_path):
        print(f"✗ Fichier introuvable: {photo_path}")
        return None

    try:
        # Lire la photo en binaire
        with open(photo_path, 'rb') as file:
            photo_blob = file.read()

        # Limiter la taille (max 5 Mo pour test)
        if len(photo_blob) > 5 * 1024 * 1024:
            print(f"✗ Photo trop grande: {len(photo_blob) / (1024*1024):.1f} Mo (max 5 Mo)")
            return None

        cursor = conn.cursor()

        # Structure correcte de la table Donnees:
        # - idDonnee_PK (int) - auto-incrémenté
        # - dateHeure (datetime)
        # - idCapteur (int) - FK vers Capteur
        # - mesure (float) - NULL pour les photos
        # - photoBlob (varbinary) - données binaires de l'image
        # - noSalle (int) - FK vers Salle (requis)

        query = """
            INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
            VALUES (GETDATE(), ?, NULL, ?, ?)
        """

        try:
            cursor.execute(query, (id_capteur, photo_blob, no_salle))
            conn.commit()

            # Récupérer l'ID inséré
            cursor.execute("SELECT @@IDENTITY")
            id_donnees = cursor.fetchone()[0]

            nom_fichier = os.path.basename(photo_path)
            taille_ko = len(photo_blob) / 1024

            print(f"✓ {nom_fichier}")
            print(f"  ID: {int(id_donnees)} | Taille: {taille_ko:.1f} Ko | Salle: {no_salle}")

            cursor.close()
            return int(id_donnees)

        except pyodbc.Error as e:
            conn.rollback()
            print(f"✗ Erreur lors de l'insertion: {e}")

            # Afficher la structure pour debug
            print(f"\nStructure de la table Donnees:")
            try:
                cursor.execute("SELECT TOP 1 * FROM Donnees")
                for col in cursor.description:
                    print(f"  - {col[0]} (type: {col[1].__name__})")
            except:
                pass

            cursor.close()
            return None

    except Exception as e:
        print(f"✗ Erreur générale: {e}")
        import traceback
        traceback.print_exc()
        return None


def lister_screenshots():
    """Liste tous les screenshots disponibles"""
    if not os.path.exists(SCREENSHOTS_DIR):
        print(f"✗ Dossier introuvable: {SCREENSHOTS_DIR}")
        return []

    # Chercher les images (PNG, JPG, JPEG)
    extensions = ['*.png', '*.jpg', '*.jpeg', '*.PNG', '*.JPG', '*.JPEG']
    photos = []

    for ext in extensions:
        pattern = os.path.join(SCREENSHOTS_DIR, ext)
        photos.extend(glob(pattern))

    return sorted(photos)


def main():
    """Fonction principale"""
    print("=" * 70)
    print("  INSERTION DE SCREENSHOTS DANS LA BASE DE DONNÉES")
    print("=" * 70)

    # Vérifier le dossier
    print(f"\nDossier: {SCREENSHOTS_DIR}")
    screenshots = lister_screenshots()

    if not screenshots:
        print("✗ Aucun screenshot trouvé!")
        print(f"  Vérifiez que le dossier existe et contient des images PNG/JPG")
        return

    print(f"✓ {len(screenshots)} screenshot(s) trouvé(s)\n")

    # Connexion à la BD
    conn = get_connection()
    if not conn:
        return

    # Lister les capteurs CAMERA
    capteurs = lister_capteurs_camera(conn)

    if not capteurs:
        print("\n✗ Aucun capteur CAMERA dans la base de données")
        print("  Vous devez d'abord créer un capteur de type CAMERA")
        conn.close()
        return

    # Utiliser le premier capteur CAMERA
    id_capteur = capteurs[0][0]
    print(f"\nUtilisation du capteur ID: {id_capteur}\n")

    # Demander combien de photos insérer
    print(f"Nombre de screenshots disponibles: {len(screenshots)}")
    choix = input(f"Combien de photos voulez-vous insérer? (Entrée = {NB_SCREENSHOTS_PAR_DEFAUT}, 'all' = tout): ").strip()

    if choix == "":
        nb_a_inserer = min(NB_SCREENSHOTS_PAR_DEFAUT, len(screenshots))
    elif choix.lower() == "all":
        nb_a_inserer = len(screenshots)
    else:
        try:
            nb_a_inserer = int(choix)
            nb_a_inserer = min(nb_a_inserer, len(screenshots))
        except ValueError:
            print(f"✗ Nombre invalide, insertion de {NB_SCREENSHOTS_PAR_DEFAUT} photos par défaut")
            nb_a_inserer = min(NB_SCREENSHOTS_PAR_DEFAUT, len(screenshots))

    # Insertion
    print(f"\n{'=' * 70}")
    print(f"  INSERTION DE {nb_a_inserer} PHOTO(S)")
    print("=" * 70 + "\n")

    ids_inseres = []
    erreurs = 0

    for i, photo_path in enumerate(screenshots[:nb_a_inserer], 1):
        print(f"[{i}/{nb_a_inserer}] ", end="")
        id_donnees = inserer_photo_blob(conn, photo_path, id_capteur)

        if id_donnees:
            ids_inseres.append(id_donnees)
        else:
            erreurs += 1
        print()

    # Résumé
    print("=" * 70)
    print(f"  RÉSUMÉ")
    print("=" * 70)
    print(f"  ✓ Photos insérées: {len(ids_inseres)}")
    print(f"  ✗ Erreurs: {erreurs}")

    if ids_inseres:
        print(f"\n  IDs insérés: {', '.join(map(str, ids_inseres))}")

    conn.close()
    print("\n" + "=" * 70 + "\n")


if __name__ == "__main__":
    main()