-- =============================================
-- Script d'insertion de 3 salles de test
-- =============================================

USE Prog3A25_bdSalleSense;
GO

-- Vérifier si des salles existent déjà
IF EXISTS (SELECT 1 FROM Salle WHERE numero IN ('A-101', 'B-205', 'C-310'))
BEGIN
    PRINT 'Les salles existent déjà. Suppression des anciennes données...';
    DELETE FROM Salle WHERE numero IN ('A-101', 'B-205', 'C-310');
END
GO

-- Insertion des 3 salles
INSERT INTO Salle (numero, capaciteMaximale) VALUES
    ('A-101', 25),  -- Petite salle de 25 personnes
    ('B-205', 40),  -- Salle moyenne de 40 personnes
    ('C-310', 60);  -- Grande salle de 60 personnes
GO

-- Afficher les salles insérées
SELECT
    idSalle_PK AS 'ID Salle',
    numero AS 'Numéro',
    capaciteMaximale AS 'Capacité Maximale'
FROM Salle
WHERE numero IN ('A-101', 'B-205', 'C-310');
GO

PRINT '✓ 3 salles insérées avec succès!';
GO
