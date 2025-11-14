-- =============================================
-- Procédure stockée pour débloquer un utilisateur
-- =============================================

USE Prog3A25_bdSalleSense;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Utilisateur_Debloquer
    @IdUtilisateur INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Vérifier si l'utilisateur existe
        IF NOT EXISTS (SELECT 1 FROM Utilisateur WHERE idUtilisateur_PK = @IdUtilisateur)
        BEGIN
            ROLLBACK;
            RETURN -1;  -- Utilisateur introuvable
        END

        -- Vérifier s'il est banni
        IF NOT EXISTS (SELECT 1 FROM Blacklist WHERE idUtilisateur = @IdUtilisateur)
        BEGIN
            ROLLBACK;
            RETURN -2;  -- N'est pas banni
        END

        -- Retirer de la blacklist
        DELETE FROM Blacklist
        WHERE idUtilisateur = @IdUtilisateur;

        COMMIT TRANSACTION;
        RETURN 0;  -- Succès

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        RETURN -99;  -- Erreur système
    END CATCH
END;
GO

PRINT '✓ Procédure stockée usp_Utilisateur_Debloquer créée avec succès!';
GO
