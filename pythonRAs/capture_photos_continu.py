"""
Script de capture continue de photos avec la Pi Camera
Les photos sont prises toutes les 5 secondes et envoyées vers la BD
"""

import time
from datetime import datetime
from io import BytesIO
from db_connection import DatabaseConnection
from config import DB_SERVER, DB_NAME, DB_USERNAME, DB_PASSWORD, ID_SALLE

try:
    from picamera2 import Picamera2
    CAMERA_AVAILABLE = True
except ImportError:
    print("⚠ picamera2 non disponible")
    CAMERA_AVAILABLE = False


class CapturePhotosContinu:
    """Capture des photos en continu et les envoie vers la BD"""

    def __init__(self, db_connection: DatabaseConnection, id_salle: int, intervalle: int = 5):
        """
        Initialise le système de capture

        Args:
            db_connection: Connexion à la base de données
            id_salle: ID de la salle à monitorer
            intervalle: Intervalle en secondes entre chaque photo (défaut: 5)
        """
        self.db = db_connection
        self.id_salle = id_salle
        self.intervalle = intervalle
        self.camera = None
        self.id_capteur_camera = None
        self.compteur_photos = 0

    def setup(self):
        """Configure la caméra et récupère l'ID du capteur"""
        print("=== Configuration du système de capture ===\n")

        # 1. Récupérer l'ID du capteur caméra
        try:
            capteur = self.db.execute_query(
                "SELECT idCapteur_PK FROM Capteur WHERE type = 'CAMERA'"
            )

            if not capteur:
                print("✗ Aucun capteur CAMERA trouvé dans la BD")
                print("   Lancez d'abord: python initialiser_bd.py")
                return False

            self.id_capteur_camera = capteur[0][0]
            print(f"✓ Capteur CAMERA trouvé - ID: {self.id_capteur_camera}")

        except Exception as e:
            print(f"✗ Erreur lors de la récupération du capteur: {e}")
            return False

        # 2. Initialiser la caméra
        if not CAMERA_AVAILABLE:
            print("✗ picamera2 n'est pas installé ou importable")
            print("  Installation: sudo apt install python3-picamera2")
            return False

        try:
            self.camera = Picamera2()

            # Configuration pour capture d'images JPEG
            config = self.camera.create_still_configuration(
                main={"size": (1920, 1080)},  # Résolution Full HD
                buffer_count=2
            )
            self.camera.configure(config)
            self.camera.start()

            print("✓ Pi Camera initialisée (1920x1080)")

            # Temps de stabilisation de la caméra
            print("⏳ Stabilisation de la caméra (2 secondes)...")
            time.sleep(2)

        except Exception as e:
            print(f"✗ Erreur lors de l'initialisation de la caméra: {e}")
            print("\n  Diagnostic:")
            print("  1. Vérifier détection: vcgencmd get_camera")
            print("  2. Lister caméras: rpicam-hello --list-cameras")
            print("  3. Vérifier câble nappe sur port CSI")
            print("  4. Redémarrer après branchement: sudo reboot")
            return False

        print("\n✓ Configuration terminée\n")
        return True

    def capturer_photo(self) -> bytes:
        """
        Capture une photo directement en mémoire

        Returns:
            Bytes de l'image JPEG (ou None si erreur)
        """
        if not CAMERA_AVAILABLE or not self.camera:
            print("✗ Caméra non disponible")
            return None

        try:
            # Capturer directement dans un BytesIO (en mémoire)
            buffer = BytesIO()
            self.camera.capture_file(buffer, format='jpeg')
            buffer.seek(0)
            return buffer.read()

        except Exception as e:
            print(f"✗ Erreur lors de la capture: {e}")
            return None

    def envoyer_photo_bd(self, photo_blob: bytes) -> bool:
        """
        Envoie la photo vers la base de données

        Args:
            photo_blob: Bytes de l'image JPEG

        Returns:
            True si succès, False sinon
        """
        if not photo_blob:
            print("✗ Photo vide")
            return False

        try:
            date_heure = datetime.now()

            # CRITIQUE: Créer un NOUVEAU cursor à chaque appel
            cursor = self.db.connection.cursor()

            # Insérer la photo dans la BD
            query = """
                INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
                VALUES (GETDATE(), ?, NULL, ?, ?)
            """

            cursor.execute(query, (self.id_capteur_camera, photo_blob, self.id_salle))
            self.db.connection.commit()

            # Récupérer l'ID de la donnée insérée
            cursor.execute("SELECT @@IDENTITY")
            id_donnee = cursor.fetchone()[0]

            # Créer un événement
            cursor.execute(
                """INSERT INTO Evenement (type, idDonnee, description)
                   VALUES (?, ?, ?)""",
                ('CAPTURE', int(id_donnee), f'Photo capturée à {date_heure.strftime("%H:%M:%S")}')
            )
            self.db.connection.commit()

            # CRITIQUE: Fermer le cursor
            cursor.close()

            self.compteur_photos += 1
            taille_kb = len(photo_blob) / 1024

            print(f"[{date_heure.strftime('%H:%M:%S')}] Photo #{self.compteur_photos} envoyée "
                  f"({taille_kb:.1f} KB) - ID: {int(id_donnee)}")

            return True

        except Exception as e:
            print(f"✗ Erreur lors de l'envoi: {e}")
            import traceback
            traceback.print_exc()
            self.db.connection.rollback()
            return False

    def capturer_en_continu(self):
        """Boucle principale de capture continue"""
        print("╔═══════════════════════════════════════════════════════════╗")
        print("║      Capture de photos en continu - Pi Camera V2         ║")
        print("╚═══════════════════════════════════════════════════════════╝\n")
        print(f"📷 Intervalle: {self.intervalle} secondes")
        print(f"🏢 Salle: {self.id_salle}")
        print(f"💾 Stockage: Base de données (VARBINARY)")
        print("\nAppuyez sur Ctrl+C pour arrêter\n")
        print("─" * 63)

        try:
            while True:
                # Capturer la photo en mémoire
                photo_blob = self.capturer_photo()

                if photo_blob:
                    # Envoyer directement vers la BD
                    self.envoyer_photo_bd(photo_blob)
                else:
                    print("✗ Échec de la capture")

                # Attendre avant la prochaine capture
                time.sleep(self.intervalle)

        except KeyboardInterrupt:
            print("\n\n" + "─" * 63)
            print(f"\n✓ Arrêt demandé - {self.compteur_photos} photos capturées")
            print("✓ Programme terminé")

    def cleanup(self):
        """Nettoie les ressources (caméra)"""
        if self.camera:
            try:
                self.camera.stop()
                self.camera.close()
                print("✓ Caméra fermée proprement")
            except:
                pass


def main():
    """Fonction principale"""
    print("\n╔═══════════════════════════════════════════════════════════╗")
    print("║        SalleSense - Capture Photos en Continu            ║")
    print("╚═══════════════════════════════════════════════════════════╝\n")

    # Connexion à la base de données
    db = DatabaseConnection(DB_SERVER, DB_NAME, DB_USERNAME, DB_PASSWORD)

    if not db.connect():
        print("\n✗ Impossible de se connecter à la base de données")
        return 1

    # Créer le système de capture
    capture_system = CapturePhotosContinu(db, ID_SALLE, intervalle=5)

    # Configuration
    if not capture_system.setup():
        db.disconnect()
        return 1

    try:
        # Lancer la capture continue
        capture_system.capturer_en_continu()

    finally:
        # Nettoyage
        capture_system.cleanup()
        db.disconnect()
        print("✓ Connexion BD fermée\n")

    return 0


if __name__ == "__main__":
    exit(main())
