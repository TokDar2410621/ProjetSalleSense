-- =============================================
-- Script de réinitialisation du mot de passe Admin (CORRECT)
-- Utilise le système salt + hash comme la procédure stockée
-- =============================================

USE Prog3A25_bdSalleSense;
GO

DECLARE @NouveauMotDePasse NVARCHAR(255) = 'admin123';
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
    -- Générer un nouveau salt (16 bytes aléatoires)
    DECLARE @Salt VARBINARY(16) = CRYPT_GEN_RANDOM(16);

    -- Calculer le hash avec le salt (comme dans la procédure stockée)
    DECLARE @Hash VARBINARY(32) = HASHBYTES('SHA2_256', @Salt + CONVERT(VARBINARY(4000), @NouveauMotDePasse));

    -- Mettre à jour avec le salt et le hash
    UPDATE Utilisateur
    SET
        mdp_salt = @Salt,
        mdp_hash = @Hash,
        motDePasse = @Hash  -- On met aussi le hash dans motDePasse pour compatibilité
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
