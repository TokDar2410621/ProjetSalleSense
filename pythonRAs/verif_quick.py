"""
Vérification rapide des photos dans la BD
"""
import pyodbc

SERVER = "(localdb)\\MSSQLLocalDB"
DATABASE = "Prog3A25_bdSalleSense"

try:
    conn = pyodbc.connect(
        f"DRIVER={{ODBC Driver 17 for SQL Server}};"
        f"SERVER={SERVER};"
        f"DATABASE={DATABASE};"
        f"Integrated Security=true;"
        f"TrustServerCertificate=yes;"
    )

    cursor = conn.cursor()

    # Compter le total de lignes
    cursor.execute("SELECT COUNT(*) FROM Donnees")
    total = cursor.fetchone()[0]
    print(f"Total lignes Donnees: {total}")

    # Compter les photos (photoBlob NOT NULL)
    cursor.execute("SELECT COUNT(*) FROM Donnees WHERE photoBlob IS NOT NULL")
    photos = cursor.fetchone()[0]
    print(f"Photos (photoBlob NOT NULL): {photos}")

    # Lister quelques photos
    if photos > 0:
        cursor.execute("""
            SELECT TOP 5
                idDonnee_PK,
                dateHeure,
                idCapteur,
                noSalle,
                DATALENGTH(photoBlob) AS taille
            FROM Donnees
            WHERE photoBlob IS NOT NULL
            ORDER BY dateHeure DESC
        """)

        print("\nDernières photos:")
        for row in cursor.fetchall():
            print(f"  ID: {row[0]}, Date: {row[1]}, Capteur: {row[2]}, Salle: {row[3]}, Taille: {row[4]} bytes")
    else:
        print("\n⚠ AUCUNE PHOTO dans la base de données!")
        print("Vous devez exécuter: python inserer_screenshots.py")

    conn.close()

except Exception as e:
    print(f"Erreur: {e}")