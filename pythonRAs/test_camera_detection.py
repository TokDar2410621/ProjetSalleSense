#!/usr/bin/env python3
"""
Script de diagnostic pour vérifier la détection de la Pi Camera
Identifie pourquoi CAMERA_AVAILABLE = False
"""

import sys
import subprocess

def check_python_version():
    """Vérifie la version de Python"""
    print("=" * 70)
    print("  1. VERSION PYTHON")
    print("=" * 70)
    print(f"Version Python: {sys.version}")
    print(f"Exécutable: {sys.executable}\n")


def check_picamera2_installation():
    """Vérifie si picamera2 est installé"""
    print("=" * 70)
    print("  2. INSTALLATION DE PICAMERA2")
    print("=" * 70)

    try:
        import picamera2
        print(f"✓ picamera2 est installé")
        print(f"  Version: {picamera2.__version__ if hasattr(picamera2, '__version__') else 'Inconnue'}")
        print(f"  Chemin: {picamera2.__file__}\n")
        return True
    except ImportError as e:
        print(f"✗ picamera2 N'EST PAS installé")
        print(f"  Erreur: {e}\n")
        print("  Solution:")
        print("    sudo apt update")
        print("    sudo apt install -y python3-picamera2\n")
        return False


def check_libcamera():
    """Vérifie si libcamera fonctionne"""
    print("=" * 70)
    print("  3. LIBCAMERA (bibliothèque système)")
    print("=" * 70)

    try:
        result = subprocess.run(
            ["libcamera-hello", "--list-cameras"],
            capture_output=True,
            text=True,
            timeout=10
        )

        if result.returncode == 0:
            print("✓ libcamera fonctionne")
            print(f"\n{result.stdout}")
            return True
        else:
            print("✗ libcamera a échoué")
            print(f"  Code retour: {result.returncode}")
            print(f"  Erreur: {result.stderr}\n")
            return False

    except FileNotFoundError:
        print("✗ libcamera-hello introuvable")
        print("  La caméra n'est peut-être pas activée\n")
        print("  Solution:")
        print("    sudo raspi-config")
        print("    → Interface Options → Camera → Enable\n")
        return False
    except subprocess.TimeoutExpired:
        print("✗ Timeout - la commande a pris trop de temps\n")
        return False
    except Exception as e:
        print(f"✗ Erreur inattendue: {e}\n")
        return False


def check_camera_device():
    """Vérifie si /dev/video0 existe"""
    print("=" * 70)
    print("  4. PÉRIPHÉRIQUE CAMÉRA (/dev/video*)")
    print("=" * 70)

    import os
    import glob

    video_devices = glob.glob("/dev/video*")

    if video_devices:
        print(f"✓ Périphériques trouvés: {', '.join(video_devices)}")

        for device in video_devices:
            if os.access(device, os.R_OK):
                print(f"  ✓ {device} est accessible en lecture")
            else:
                print(f"  ✗ {device} N'EST PAS accessible (permissions?)")
    else:
        print("✗ Aucun périphérique /dev/video* trouvé")
        print("  La caméra n'est probablement pas détectée par le système\n")

    print()


def check_user_groups():
    """Vérifie si l'utilisateur est dans le groupe 'video'"""
    print("=" * 70)
    print("  5. PERMISSIONS UTILISATEUR")
    print("=" * 70)

    import os
    import grp

    username = os.getenv("USER")
    print(f"Utilisateur: {username}")

    try:
        video_group = grp.getgrnam("video")
        if username in video_group.gr_mem:
            print(f"✓ {username} est dans le groupe 'video'\n")
            return True
        else:
            print(f"✗ {username} N'EST PAS dans le groupe 'video'")
            print("  Solution:")
            print(f"    sudo usermod -a -G video {username}")
            print("    puis redémarrer la session\n")
            return False
    except KeyError:
        print("⚠ Groupe 'video' introuvable\n")
        return False


def test_picamera2_init():
    """Tente d'initialiser Picamera2"""
    print("=" * 70)
    print("  6. TEST INITIALISATION PICAMERA2")
    print("=" * 70)

    try:
        from picamera2 import Picamera2
        print("Import réussi - Tentative d'initialisation...\n")

        camera = Picamera2()
        print(f"✓ Picamera2 initialisé avec succès!")
        print(f"  Sensor modes: {camera.sensor_modes}\n")

        camera.close()
        return True

    except ImportError:
        print("✗ Impossible d'importer picamera2\n")
        return False
    except Exception as e:
        print(f"✗ Échec de l'initialisation: {e}")
        print(f"  Type: {type(e).__name__}\n")

        if "Permission denied" in str(e):
            print("  → Problème de permissions - essayez avec sudo:")
            print("    sudo python3 test_camera_detection.py\n")
        elif "No cameras available" in str(e):
            print("  → Aucune caméra détectée par le système")
            print("    Vérifiez que la caméra est bien connectée")
            print("    et activée dans raspi-config\n")

        return False


def check_camera_config():
    """Vérifie la config de la caméra dans /boot/config.txt"""
    print("=" * 70)
    print("  7. CONFIGURATION /boot/config.txt")
    print("=" * 70)

    config_paths = ["/boot/config.txt", "/boot/firmware/config.txt"]

    for path in config_paths:
        try:
            with open(path, 'r') as f:
                content = f.read()

            print(f"Fichier: {path}")

            # Vérifier les lignes importantes
            if "camera_auto_detect=1" in content:
                print("  ✓ camera_auto_detect=1 (activé)")
            else:
                print("  ⚠ camera_auto_detect non trouvé")

            if "start_x=1" in content:
                print("  ✓ start_x=1 (GPU mémoire pour caméra)")

            if "gpu_mem=" in content:
                import re
                match = re.search(r"gpu_mem=(\d+)", content)
                if match:
                    gpu_mem = int(match.group(1))
                    if gpu_mem >= 128:
                        print(f"  ✓ gpu_mem={gpu_mem} (suffisant)")
                    else:
                        print(f"  ⚠ gpu_mem={gpu_mem} (recommandé: 128+)")

            print()
            return True

        except FileNotFoundError:
            continue
        except PermissionError:
            print(f"✗ Accès refusé à {path} (essayez avec sudo)\n")
            return False

    print("✗ Aucun fichier config.txt trouvé\n")
    return False


def main():
    """Fonction principale"""
    print("\n╔" + "═" * 68 + "╗")
    print("║" + " " * 15 + "DIAGNOSTIC PI CAMERA - DÉTECTION" + " " * 20 + "║")
    print("╚" + "═" * 68 + "╝\n")

    results = {}

    check_python_version()
    results['picamera2'] = check_picamera2_installation()
    results['libcamera'] = check_libcamera()
    check_camera_device()
    results['permissions'] = check_user_groups()

    if results['picamera2']:
        results['init'] = test_picamera2_init()
    else:
        results['init'] = False

    check_camera_config()

    # Résumé
    print("=" * 70)
    print("  RÉSUMÉ DU DIAGNOSTIC")
    print("=" * 70)
    print(f"  picamera2 installé: {'✓' if results['picamera2'] else '✗'}")
    print(f"  libcamera fonctionne: {'✓' if results['libcamera'] else '✗'}")
    print(f"  Permissions OK: {'✓' if results['permissions'] else '✗'}")
    print(f"  Initialisation réussie: {'✓' if results['init'] else '✗'}")
    print("=" * 70 + "\n")

    if all(results.values()):
        print("✅ TOUT FONCTIONNE - La caméra devrait être détectée!")
        print("   Si capture_photos_continu.py affiche encore 'mode simulation',")
        print("   essayez de le lancer avec sudo:\n")
        print("   sudo python3 capture_photos_continu.py\n")
    else:
        print("❌ PROBLÈMES DÉTECTÉS - Actions recommandées:\n")

        if not results['picamera2']:
            print("  1. Installer picamera2:")
            print("     sudo apt update")
            print("     sudo apt install -y python3-picamera2\n")

        if not results['libcamera']:
            print("  2. Activer la caméra:")
            print("     sudo raspi-config")
            print("     → Interface Options → Camera → Enable")
            print("     puis redémarrer (sudo reboot)\n")

        if not results['permissions']:
            print("  3. Ajouter l'utilisateur au groupe video:")
            print(f"     sudo usermod -a -G video {os.getenv('USER')}")
            print("     puis redémarrer la session\n")

        if not results['init']:
            print("  4. Essayer avec sudo:")
            print("     sudo python3 capture_photos_continu.py\n")


if __name__ == "__main__":
    import os
    main()
