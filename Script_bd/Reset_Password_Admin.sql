-- =============================================
-- Script de réinitialisation du mot de passe Admin
-- Pour réinitialiser le mot de passe de tokamdaruis@gmail.com
-- =============================================

USE Prog3A25_bdSalleSense;
GO

-- =============================================
-- INSTRUCTIONS:
-- 1. Remplacez 'VOTRE_NOUVEAU_MOT_DE_PASSE' par le mot de passe souhaité
-- 2. Exécutez ce script
-- =============================================

DECLARE @NouveauMotDePasse NVARCHAR(255) = 'admin123';  -- ⚠️ CHANGEZ CE MOT DE PASSE!
DECLARE @Courriel NVARCHAR(255) = 'tokamdaruis@gmail.com';
DECLARE @UserId INT;

-- Vérifier que l'utilisateur existe
SELECT @UserId = idUtilisateur_PK
FROM Utilisateur
WHERE courriel = @Courriel;

IF @UserId IS NULL
BEGIN
    PRINT '❌ Erreur: Utilisateur non trouvé avec le courriel ' + @Courriel;
END
ELSE
BEGIN
    -- Réinitialiser le mot de passe (hashé par SQL Server)
    UPDATE Utilisateur
    SET motDePasse = HASHBYTES('SHA2_256', @NouveauMotDePasse)
    WHERE idUtilisateur_PK = @UserId;

    PRINT '✓ Mot de passe réinitialisé avec succès!';
    PRINT '';
    PRINT '📧 Courriel: ' + @Courriel;
    PRINT '🔑 Nouveau mot de passe: ' + @NouveauMotDePasse;
    PRINT '👑 Rôle: Admin';
    PRINT '';
    PRINT '⚠️ IMPORTANT: Notez bien votre nouveau mot de passe!';
END
GO
