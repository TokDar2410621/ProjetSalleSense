-- =============================================
-- Script pour supprimer des réservations
-- =============================================

USE Prog3A25_bdSalleSense;
GO

-- Afficher les réservations existantes
PRINT '=== RÉSERVATIONS EXISTANTES ===';
SELECT
    idReservation_PK,
    heureDebut,
    heureFin,
    noSalle,
    noPersonne,
    nombrePersonne
FROM Reservation
ORDER BY heureDebut DESC;

PRINT '';
PRINT '=== SUPPRESSION DES RÉSERVATIONS ===';

-- Option 1: Supprimer TOUTES les réservations
-- Décommentez la ligne suivante pour tout supprimer
-- DELETE FROM Reservation;

-- Option 2: Supprimer les réservations futures seulement
-- DELETE FROM Reservation WHERE heureDebut > GETDATE();

-- Option 3: Supprimer les 10 dernières réservations
-- DELETE FROM Reservation WHERE idReservation_PK IN (
--     SELECT TOP 10 idReservation_PK FROM Reservation ORDER BY idReservation_PK DESC
-- );

-- Option 4: Supprimer les réservations d'un utilisateur spécifique
-- DELETE FROM Reservation WHERE noPersonne = 1; -- Remplacer 1 par l'ID utilisateur

PRINT 'Script prêt. Décommentez l''option souhaitée pour supprimer les réservations.';
GO
