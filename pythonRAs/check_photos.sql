-- Script SQL pour vérifier les photos dans la BD
USE Prog3A25_bdSalleSense;
GO

-- 1. Nombre total de lignes dans Donnees
SELECT 'Total lignes Donnees' AS Info, COUNT(*) AS Nombre
FROM Donnees;

-- 2. Nombre de photos (photoBlob NOT NULL)
SELECT 'Photos (photoBlob NOT NULL)' AS Info, COUNT(*) AS Nombre
FROM Donnees
WHERE photoBlob IS NOT NULL;

-- 3. Détails des photos
SELECT TOP 10
    idDonnee_PK,
    dateHeure,
    idCapteur,
    noSalle,
    DATALENGTH(photoBlob) AS TailleBytes,
    DATALENGTH(photoBlob) / 1024 AS TailleKB
FROM Donnees
WHERE photoBlob IS NOT NULL
ORDER BY dateHeure DESC;

-- 4. Vérifier les capteurs CAMERA
SELECT c.idCapteur_PK, c.nom, c.type, COUNT(d.idDonnee_PK) AS NombrePhotos
FROM Capteur c
LEFT JOIN Donnees d ON d.idCapteur = c.idCapteur_PK AND d.photoBlob IS NOT NULL
WHERE c.type = 'CAMERA'
GROUP BY c.idCapteur_PK, c.nom, c.type;