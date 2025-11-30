-- Script pour insérer des événements de test
-- Types valides: ENTREE, SORTIE, BRUIT_FORT, BRUIT_FAIBLE, CAPTURE,
--                MOUVEMENT_DETECTE, TEMPERATURE_HAUTE, TEMPERATURE_BASSE,
--                ALERTE_CO2, HUMIDITE_ANORMALE

USE Prog3A25_bdSalleSense;
GO

DECLARE @NoSalle1 INT = 1;
DECLARE @NoSalle2 INT = 2;
DECLARE @NoSalle3 INT = 3;

-- Récupérer un capteur existant
DECLARE @IdCapteur INT = (SELECT TOP 1 idCapteur_PK FROM Capteur);

-- Insérer des données de capteurs si nécessaire
IF NOT EXISTS (SELECT 1 FROM Donnees WHERE noSalle = @NoSalle1 AND dateHeure > DATEADD(DAY, -7, GETDATE()))
BEGIN
    INSERT INTO Donnees (dateHeure, idCapteur, mesure, noSalle)
    VALUES
        (DATEADD(HOUR, -1, GETDATE()), @IdCapteur, 1.0, @NoSalle1),
        (DATEADD(HOUR, -3, GETDATE()), @IdCapteur, 75.5, @NoSalle1),
        (DATEADD(HOUR, -5, GETDATE()), @IdCapteur, 1.0, @NoSalle1),
        (DATEADD(DAY, -1, GETDATE()), @IdCapteur, NULL, @NoSalle1),
        (DATEADD(DAY, -2, GETDATE()), @IdCapteur, 82.3, @NoSalle1);
END

IF NOT EXISTS (SELECT 1 FROM Donnees WHERE noSalle = @NoSalle2 AND dateHeure > DATEADD(DAY, -7, GETDATE()))
BEGIN
    INSERT INTO Donnees (dateHeure, idCapteur, mesure, noSalle)
    VALUES
        (DATEADD(HOUR, -2, GETDATE()), @IdCapteur, 1.0, @NoSalle2),
        (DATEADD(HOUR, -6, GETDATE()), @IdCapteur, 68.2, @NoSalle2),
        (DATEADD(DAY, -1, GETDATE()), @IdCapteur, 1.0, @NoSalle2);
END

IF NOT EXISTS (SELECT 1 FROM Donnees WHERE noSalle = @NoSalle3 AND dateHeure > DATEADD(DAY, -7, GETDATE()))
BEGIN
    INSERT INTO Donnees (dateHeure, idCapteur, mesure, noSalle)
    VALUES
        (DATEADD(HOUR, -4, GETDATE()), @IdCapteur, NULL, @NoSalle3),
        (DATEADD(DAY, -3, GETDATE()), @IdCapteur, 90.1, @NoSalle3);
END

-- Insérer les événements avec les types valides
-- Salle 1
INSERT INTO Evenement (type, idDonnee, description)
SELECT TOP 1 'MOUVEMENT_DETECTE', idDonnee_PK, 'Mouvement détecté dans la salle'
FROM Donnees WHERE noSalle = @NoSalle1 AND NOT EXISTS (SELECT 1 FROM Evenement WHERE idDonnee = Donnees.idDonnee_PK)
ORDER BY dateHeure DESC;

INSERT INTO Evenement (type, idDonnee, description)
SELECT TOP 1 'BRUIT_FORT', idDonnee_PK, 'Niveau sonore élevé détecté'
FROM Donnees WHERE noSalle = @NoSalle1 AND NOT EXISTS (SELECT 1 FROM Evenement WHERE idDonnee = Donnees.idDonnee_PK)
ORDER BY dateHeure DESC;

INSERT INTO Evenement (type, idDonnee, description)
SELECT TOP 1 'CAPTURE', idDonnee_PK, 'Capture photo automatique'
FROM Donnees WHERE noSalle = @NoSalle1 AND NOT EXISTS (SELECT 1 FROM Evenement WHERE idDonnee = Donnees.idDonnee_PK)
ORDER BY dateHeure DESC;

INSERT INTO Evenement (type, idDonnee, description)
SELECT TOP 1 'ENTREE', idDonnee_PK, 'Entrée détectée dans la salle'
FROM Donnees WHERE noSalle = @NoSalle1 AND NOT EXISTS (SELECT 1 FROM Evenement WHERE idDonnee = Donnees.idDonnee_PK)
ORDER BY dateHeure DESC;

-- Salle 2
INSERT INTO Evenement (type, idDonnee, description)
SELECT TOP 1 'MOUVEMENT_DETECTE', idDonnee_PK, 'Présence détectée'
FROM Donnees WHERE noSalle = @NoSalle2 AND NOT EXISTS (SELECT 1 FROM Evenement WHERE idDonnee = Donnees.idDonnee_PK)
ORDER BY dateHeure DESC;

INSERT INTO Evenement (type, idDonnee, description)
SELECT TOP 1 'SORTIE', idDonnee_PK, 'Sortie détectée'
FROM Donnees WHERE noSalle = @NoSalle2 AND NOT EXISTS (SELECT 1 FROM Evenement WHERE idDonnee = Donnees.idDonnee_PK)
ORDER BY dateHeure DESC;

INSERT INTO Evenement (type, idDonnee, description)
SELECT TOP 1 'BRUIT_FAIBLE', idDonnee_PK, 'Bruit faible détecté'
FROM Donnees WHERE noSalle = @NoSalle2 AND NOT EXISTS (SELECT 1 FROM Evenement WHERE idDonnee = Donnees.idDonnee_PK)
ORDER BY dateHeure DESC;

-- Salle 3
INSERT INTO Evenement (type, idDonnee, description)
SELECT TOP 1 'CAPTURE', idDonnee_PK, 'Photo de surveillance'
FROM Donnees WHERE noSalle = @NoSalle3 AND NOT EXISTS (SELECT 1 FROM Evenement WHERE idDonnee = Donnees.idDonnee_PK)
ORDER BY dateHeure DESC;

INSERT INTO Evenement (type, idDonnee, description)
SELECT TOP 1 'TEMPERATURE_HAUTE', idDonnee_PK, 'Température élevée détectée'
FROM Donnees WHERE noSalle = @NoSalle3 AND NOT EXISTS (SELECT 1 FROM Evenement WHERE idDonnee = Donnees.idDonnee_PK)
ORDER BY dateHeure DESC;

-- Afficher les événements
SELECT e.idEvenement_PK, e.type, e.description, d.dateHeure, d.noSalle
FROM Evenement e
INNER JOIN Donnees d ON e.idDonnee = d.idDonnee_PK
ORDER BY d.noSalle, d.dateHeure DESC;

PRINT 'Événements de test insérés avec succès!';
GO
