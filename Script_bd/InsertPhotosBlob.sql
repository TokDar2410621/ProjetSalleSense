-- ==========================================
--  Script d'insertion de photos en BLOB
-- ==========================================
USE Prog3A25_bdSalleSense;
GO

-- ==========================================
-- MÉTHODE 1: Insertion depuis un fichier (nécessite accès système)
-- ==========================================
/*
-- Cette méthode nécessite d'avoir des fichiers JPG sur le serveur SQL

-- Activer Ole Automation Procedures (pour conversion base64)
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'Ole Automation Procedures', 1;
RECONFIGURE;

-- Exemple: Insérer une image depuis un fichier du serveur
INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
SELECT
    GETDATE(),
    3, -- CAM-1
    NULL,
    BulkColumn,
    1 -- Salle A-101
FROM OPENROWSET(BULK 'C:\Photos\camera_salle_A101.jpg', SINGLE_BLOB) AS Image;
*/

-- ==========================================
-- MÉTHODE 2: Insertion d'une image 1x1 pixel de test (JPG minimal)
-- ==========================================
-- Cette image est un vrai JPG de 631 bytes (1x1 pixel rouge)
DECLARE @PhotoTest VARBINARY(MAX);

-- JPG 1x1 pixel rouge (format hexadécimal)
SET @PhotoTest = 0xFFD8FFE000104A46494600010100000100010000FFDB004300080606070605080707070909080A0C140D0C0B0B0C1912130F141D1A1F1E1D1A1C1C20242E2720222C231C1C2837292C30313434341F27393D38323C2E333432FFDB0043010909090C0B0C180D0D1832211C213232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232323232FFC00011080001000103011100021101031101FFC4001500010100000000000000000000000000000000FFC40014100100000000000000000000000000000000FFC4001501010100000000000000000000000000000000FFC40014110100000000000000000000000000000000FFDA000C03010002110311003F00BF8000FFD9;

-- Insérer plusieurs photos de test pour différentes salles
-- Photo 1: Salle A-101
INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
VALUES (DATEADD(HOUR, -2, GETDATE()), 3, NULL, @PhotoTest, 1);

-- Photo 2: Salle A-101 (autre moment)
INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
VALUES (DATEADD(HOUR, -1, GETDATE()), 3, NULL, @PhotoTest, 1);

-- Photo 3: Salle B-202
INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
VALUES (DATEADD(MINUTE, -30, GETDATE()), 3, NULL, @PhotoTest, 2);

-- Photo 4: Salle C-303
INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
VALUES (DATEADD(MINUTE, -15, GETDATE()), 3, NULL, @PhotoTest, 3);

-- Photo 5: Salle C-303 (récente)
INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
VALUES (GETDATE(), 3, NULL, @PhotoTest, 3);

PRINT 'Photos de test insérées avec succès!';
GO

-- ==========================================
-- MÉTHODE 3: Procédure pour insérer une photo depuis base64
-- ==========================================
CREATE OR ALTER PROCEDURE dbo.usp_InsertPhotoFromBase64
    @Base64String NVARCHAR(MAX),
    @IdCapteur INT,
    @NoSalle INT
AS
BEGIN
    DECLARE @PhotoBlob VARBINARY(MAX);

    -- Convertir Base64 en VARBINARY
    SET @PhotoBlob = CAST('' AS XML).value('xs:base64Binary(sql:variable("@Base64String"))', 'VARBINARY(MAX)');

    -- Insérer la photo
    INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
    VALUES (GETDATE(), @IdCapteur, NULL, @PhotoBlob, @NoSalle);

    PRINT 'Photo insérée avec succès!';
END;
GO

-- ==========================================
-- VÉRIFICATION: Afficher les photos existantes
-- ==========================================
SELECT
    idDonnee_PK,
    dateHeure,
    idCapteur,
    noSalle,
    CASE
        WHEN photoBlob IS NOT NULL THEN 'OUI (' + CAST(LEN(photoBlob) AS VARCHAR) + ' bytes)'
        ELSE 'NON'
    END AS PhotoPresente,
    photo AS CheminPhoto
FROM Donnees
WHERE idCapteur = 3 -- Caméra
ORDER BY dateHeure DESC;
GO

-- ==========================================
-- DOCUMENTATION
-- ==========================================
/*
POUR AJOUTER VOS PROPRES PHOTOS:

1. Convertir votre image JPG en base64
   - En ligne: https://base64.guru/converter/encode/image
   - Ou avec PowerShell:
     $bytes = [System.IO.File]::ReadAllBytes("C:\chemin\vers\photo.jpg")
     $base64 = [System.Convert]::ToBase64String($bytes)
     $base64

2. Utiliser la procédure stockée:
   EXEC dbo.usp_InsertPhotoFromBase64
       @Base64String = '/9j/4AAQSkZJRgABAQEA...',  -- Votre base64 ici
       @IdCapteur = 3,
       @NoSalle = 1;

3. Ou directement en hexadécimal:
   INSERT INTO Donnees (dateHeure, idCapteur, mesure, photoBlob, noSalle)
   VALUES (GETDATE(), 3, NULL, 0xFFD8FFE0..., 1);  -- Hex ici
*/
