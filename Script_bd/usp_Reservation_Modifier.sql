/* ============================================================
   usp_Reservation_Modifier
   ----------------------------------------------------------
   Modifie une réservation existante avec validations
   - Admins peuvent modifier n'importe quelle réservation
   - Utilisateurs normaux peuvent modifier uniquement leurs réservations
   - Vérifie la disponibilité de la salle
   - Vérifie la capacité
   ============================================================ */
USE Prog3A25_bdSalleSense;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Reservation_Modifier
    @IdReservation INT,
    @IdUtilisateur INT,
    @EstAdmin BIT,
    @NouvelleHeureDebut DATETIME2,
    @NouvelleHeureFin DATETIME2,
    @NouveauNombrePersonnes INT,
    @CodeStatut INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @ProprietaireReservation INT;
        DECLARE @SalleId INT;
        DECLARE @CapaciteMax INT;

        -- Vérifier si la réservation existe
        SELECT @ProprietaireReservation = noPersonne, @SalleId = noSalle
        FROM Reservation
        WHERE idReservation_PK = @IdReservation;

        IF @ProprietaireReservation IS NULL
        BEGIN
            SET @CodeStatut = -1;  -- Réservation introuvable
            ROLLBACK;
            RETURN;
        END

        -- Vérifier les permissions (admin ou propriétaire)
        IF @EstAdmin = 0 AND @ProprietaireReservation != @IdUtilisateur
        BEGIN
            SET @CodeStatut = -2;  -- Pas les droits pour modifier
            ROLLBACK;
            RETURN;
        END

        -- Récupérer la capacité de la salle
        SELECT @CapaciteMax = capaciteMaximale
        FROM Salle
        WHERE idSalle_PK = @SalleId;

        IF @CapaciteMax IS NULL
        BEGIN
            SET @CodeStatut = -3;  -- Salle introuvable
            ROLLBACK;
            RETURN;
        END

        -- Vérifier la capacité
        IF @NouveauNombrePersonnes > @CapaciteMax
        BEGIN
            SET @CodeStatut = -4;  -- Capacité dépassée
            ROLLBACK;
            RETURN;
        END

        -- Vérifier les chevauchements (exclure la réservation actuelle)
        IF EXISTS (
            SELECT 1 FROM Reservation
            WHERE noSalle = @SalleId
            AND idReservation_PK != @IdReservation
            AND (
                (@NouvelleHeureDebut >= heureDebut AND @NouvelleHeureDebut < heureFin)
                OR (@NouvelleHeureFin > heureDebut AND @NouvelleHeureFin <= heureFin)
                OR (@NouvelleHeureDebut <= heureDebut AND @NouvelleHeureFin >= heureFin)
            )
        )
        BEGIN
            SET @CodeStatut = -5;  -- Salle déjà réservée pour ce créneau
            ROLLBACK;
            RETURN;
        END

        -- Mettre à jour la réservation
        UPDATE Reservation
        SET heureDebut = @NouvelleHeureDebut,
            heureFin = @NouvelleHeureFin,
            nombrePersonne = @NouveauNombrePersonnes
        WHERE idReservation_PK = @IdReservation;

        SET @CodeStatut = 0;  -- Succès
        COMMIT TRANSACTION;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        SET @CodeStatut = -99;  -- Erreur système
    END CATCH
END;
GO

/* ======= EXEMPLE D'UTILISATION =======
DECLARE @Result INT;

-- Utilisateur normal modifie sa réservation
EXEC dbo.usp_Reservation_Modifier
    @IdReservation = 1,
    @IdUtilisateur = 5,
    @EstAdmin = 0,
    @NouvelleHeureDebut = '2025-09-25 14:00:00',
    @NouvelleHeureFin = '2025-09-25 16:00:00',
    @NouveauNombrePersonnes = 8,
    @CodeStatut = @Result OUTPUT;

-- Admin modifie n'importe quelle réservation
EXEC dbo.usp_Reservation_Modifier
    @IdReservation = 1,
    @IdUtilisateur = 1,  -- ID de l'admin
    @EstAdmin = 1,
    @NouvelleHeureDebut = '2025-09-25 14:00:00',
    @NouvelleHeureFin = '2025-09-25 16:00:00',
    @NouveauNombrePersonnes = 8,
    @CodeStatut = @Result OUTPUT;

IF @Result = 0
    PRINT 'Réservation modifiée avec succès';
ELSE IF @Result = -1
    PRINT 'Réservation introuvable';
ELSE IF @Result = -2
    PRINT 'Vous n''avez pas les droits pour modifier cette réservation';
ELSE IF @Result = -3
    PRINT 'Salle introuvable';
ELSE IF @Result = -4
    PRINT 'Capacité de la salle dépassée';
ELSE IF @Result = -5
    PRINT 'Salle déjà réservée pour ce créneau';
ELSE
    PRINT 'Erreur système';
=========================================*/
