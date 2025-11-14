-- =============================================
-- Script d'insertion de capteurs et données de test
-- Pour tester l'affichage des détails de salle
-- =============================================

USE Prog3A25_bdSalleSense;
GO

-- =============================================
-- 1. INSERTION DES CAPTEURS (1 par salle, 3 types différents)
-- =============================================

-- Vérifier si les capteurs existent déjà
IF EXISTS (SELECT 1 FROM Capteur WHERE nom IN ('BRT-101', 'MVT-205', 'CAM-310'))
BEGIN
    PRINT 'Suppression des anciens capteurs de test...';
    -- Supprimer d'abord les données liées
    DELETE FROM Donnees WHERE idCapteur IN (
        SELECT idCapteur_PK FROM Capteur WHERE nom IN ('BRT-101', 'MVT-205', 'CAM-310')
    );
    -- Puis supprimer les capteurs
    DELETE FROM Capteur WHERE nom IN ('BRT-101', 'MVT-205', 'CAM-310');
END
GO

-- Récupérer les IDs des salles
DECLARE @idSalleA INT = (SELECT idSalle_PK FROM Salle WHERE numero = 'A-101');
DECLARE @idSalleB INT = (SELECT idSalle_PK FROM Salle WHERE numero = 'B-205');
DECLARE @idSalleC INT = (SELECT idSalle_PK FROM Salle WHERE numero = 'C-310');

-- Insertion des capteurs (format: XXX-### selon la contrainte)
INSERT INTO Capteur (nom, type) VALUES
    ('BRT-101', 'BRUIT'),          -- Capteur de bruit pour salle A-101
    ('MVT-205', 'MOUVEMENT'),      -- Capteur de mouvement pour salle B-205
    ('CAM-310', 'CAMERA');         -- Caméra pour salle C-310
GO

-- Afficher les capteurs insérés
SELECT
    idCapteur_PK AS 'ID Capteur',
    nom AS 'Nom',
    type AS 'Type'
FROM Capteur
WHERE nom IN ('BRT-101', 'MVT-205', 'CAM-310');
GO

-- =============================================
-- 2. INSERTION DES DONNÉES (dernières 24h)
-- =============================================

-- Variables pour les IDs
DECLARE @idCapteurA INT = (SELECT idCapteur_PK FROM Capteur WHERE nom = 'BRT-101');
DECLARE @idCapteurB INT = (SELECT idCapteur_PK FROM Capteur WHERE nom = 'MVT-205');
DECLARE @idCapteurC INT = (SELECT idCapteur_PK FROM Capteur WHERE nom = 'CAM-310');

DECLARE @idSalleA101 INT = (SELECT idSalle_PK FROM Salle WHERE numero = 'A-101');
DECLARE @idSalleB205 INT = (SELECT idSalle_PK FROM Salle WHERE numero = 'B-205');
DECLARE @idSalleC310 INT = (SELECT idSalle_PK FROM Salle WHERE numero = 'C-310');

-- Données pour Salle A-101 (Capteur de BRUIT)
-- Niveaux sonores variés sur les dernières 24h
INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle) VALUES
    (DATEADD(HOUR, -23, GETDATE()), @idCapteurA, 35.5, NULL, @idSalleA101), -- Calme
    (DATEADD(HOUR, -22, GETDATE()), @idCapteurA, 42.3, NULL, @idSalleA101), -- Normal
    (DATEADD(HOUR, -20, GETDATE()), @idCapteurA, 68.7, NULL, @idSalleA101), -- Modéré
    (DATEADD(HOUR, -18, GETDATE()), @idCapteurA, 75.2, NULL, @idSalleA101), -- Élevé
    (DATEADD(HOUR, -16, GETDATE()), @idCapteurA, 52.1, NULL, @idSalleA101), -- Normal
    (DATEADD(HOUR, -14, GETDATE()), @idCapteurA, 38.9, NULL, @idSalleA101), -- Calme
    (DATEADD(HOUR, -12, GETDATE()), @idCapteurA, 70.5, NULL, @idSalleA101), -- Modéré
    (DATEADD(HOUR, -10, GETDATE()), @idCapteurA, 82.3, NULL, @idSalleA101), -- Très élevé
    (DATEADD(HOUR, -8, GETDATE()), @idCapteurA, 45.6, NULL, @idSalleA101),  -- Normal
    (DATEADD(HOUR, -6, GETDATE()), @idCapteurA, 55.8, NULL, @idSalleA101),  -- Normal
    (DATEADD(HOUR, -4, GETDATE()), @idCapteurA, 48.2, NULL, @idSalleA101),  -- Normal
    (DATEADD(HOUR, -2, GETDATE()), @idCapteurA, 62.4, NULL, @idSalleA101),  -- Modéré
    (DATEADD(HOUR, -1, GETDATE()), @idCapteurA, 40.1, NULL, @idSalleA101);  -- Calme

-- Données pour Salle B-205 (Capteur de MOUVEMENT)
-- Détection de mouvement (1 = mouvement détecté, 0 = aucun mouvement)
INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle) VALUES
    (DATEADD(HOUR, -23, GETDATE()), @idCapteurB, 1, NULL, @idSalleB205), -- Mouvement
    (DATEADD(HOUR, -22, GETDATE()), @idCapteurB, 1, NULL, @idSalleB205), -- Mouvement
    (DATEADD(HOUR, -20, GETDATE()), @idCapteurB, 0, NULL, @idSalleB205), -- Aucun
    (DATEADD(HOUR, -18, GETDATE()), @idCapteurB, 0, NULL, @idSalleB205), -- Aucun
    (DATEADD(HOUR, -16, GETDATE()), @idCapteurB, 1, NULL, @idSalleB205), -- Mouvement
    (DATEADD(HOUR, -14, GETDATE()), @idCapteurB, 1, NULL, @idSalleB205), -- Mouvement
    (DATEADD(HOUR, -12, GETDATE()), @idCapteurB, 1, NULL, @idSalleB205), -- Mouvement
    (DATEADD(HOUR, -10, GETDATE()), @idCapteurB, 0, NULL, @idSalleB205), -- Aucun
    (DATEADD(HOUR, -8, GETDATE()), @idCapteurB, 1, NULL, @idSalleB205),  -- Mouvement
    (DATEADD(HOUR, -6, GETDATE()), @idCapteurB, 1, NULL, @idSalleB205),  -- Mouvement
    (DATEADD(HOUR, -4, GETDATE()), @idCapteurB, 0, NULL, @idSalleB205),  -- Aucun
    (DATEADD(HOUR, -2, GETDATE()), @idCapteurB, 1, NULL, @idSalleB205),  -- Mouvement
    (DATEADD(HOUR, -1, GETDATE()), @idCapteurB, 0, NULL, @idSalleB205);  -- Aucun

-- Données pour Salle C-310 (CAMÉRA - photos simulées)
-- Pour les caméras, photoBlob serait normalement rempli, mais on simule avec des chemins
INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle) VALUES
    (DATEADD(HOUR, -23, GETDATE()), @idCapteurC, NULL, NULL, @idSalleC310), -- Photo 1
    (DATEADD(HOUR, -20, GETDATE()), @idCapteurC, NULL, NULL, @idSalleC310), -- Photo 2
    (DATEADD(HOUR, -17, GETDATE()), @idCapteurC, NULL, NULL, @idSalleC310), -- Photo 3
    (DATEADD(HOUR, -14, GETDATE()), @idCapteurC, NULL, NULL, @idSalleC310), -- Photo 4
    (DATEADD(HOUR, -11, GETDATE()), @idCapteurC, NULL, NULL, @idSalleC310), -- Photo 5
    (DATEADD(HOUR, -8, GETDATE()), @idCapteurC, NULL, NULL, @idSalleC310),  -- Photo 6
    (DATEADD(HOUR, -5, GETDATE()), @idCapteurC, NULL, NULL, @idSalleC310),  -- Photo 7
    (DATEADD(HOUR, -2, GETDATE()), @idCapteurC, NULL, NULL, @idSalleC310);  -- Photo 8

GO

-- Afficher les données insérées
PRINT '=== DONNÉES CAPTEUR BRUIT (Salle A-101) ===';
SELECT TOP 5
    CONVERT(VARCHAR(19), dateHeure, 120) AS 'Date/Heure',
    mesure AS 'Niveau Sonore (dB)',
    noSalle AS 'No Salle'
FROM Donnees
WHERE idCapteur = (SELECT idCapteur_PK FROM Capteur WHERE nom = 'BRT-101')
ORDER BY dateHeure DESC;

PRINT '=== DONNÉES CAPTEUR MOUVEMENT (Salle B-205) ===';
SELECT TOP 5
    CONVERT(VARCHAR(19), dateHeure, 120) AS 'Date/Heure',
    CASE WHEN mesure = 1 THEN 'Détecté' ELSE 'Aucun' END AS 'Mouvement',
    noSalle AS 'No Salle'
FROM Donnees
WHERE idCapteur = (SELECT idCapteur_PK FROM Capteur WHERE nom = 'MVT-205')
ORDER BY dateHeure DESC;

PRINT '=== DONNÉES CAPTEUR CAMÉRA (Salle C-310) ===';
SELECT TOP 5
    CONVERT(VARCHAR(19), dateHeure, 120) AS 'Date/Heure',
    'Photo capturée' AS 'Type',
    noSalle AS 'No Salle'
FROM Donnees
WHERE idCapteur = (SELECT idCapteur_PK FROM Capteur WHERE nom = 'CAM-310')
ORDER BY dateHeure DESC;

GO

PRINT '✓ Capteurs et données insérés avec succès!';
GO
