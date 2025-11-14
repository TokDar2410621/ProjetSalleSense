-- =============================================
-- Script d'ajout du système de rôles
-- Permet de créer des administrateurs qui peuvent blacklister des users
-- =============================================

USE Prog3A25_bdSalleSense;
GO

-- =============================================
-- 1. AJOUTER LA COLONNE ROLE À LA TABLE UTILISATEUR
-- =============================================

-- Vérifier si la colonne existe déjà
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Utilisateur') AND name = 'role')
BEGIN
    ALTER TABLE Utilisateur
    ADD role NVARCHAR(20) NOT NULL DEFAULT 'User';

    PRINT '✓ Colonne "role" ajoutée à la table Utilisateur';
END
ELSE
BEGIN
    PRINT '! La colonne "role" existe déjà';
END
GO

-- =============================================
-- 2. METTRE À JOUR L'UTILISATEUR EXISTANT COMME ADMIN
-- =============================================

-- Mettre à jour l'utilisateur "leroi" comme Admin
UPDATE Utilisateur
SET role = 'Admin'
WHERE courriel = 'tokamdaruis@gmail.com';

PRINT '✓ L''utilisateur "tokamdaruis@gmail.com" est maintenant Admin';
GO

-- =============================================
-- 3. CRÉER UN UTILISATEUR NORMAL POUR TESTER
-- =============================================

-- Vérifier si l'utilisateur test existe déjà
IF NOT EXISTS (SELECT 1 FROM Utilisateur WHERE courriel = 'user.test@example.com')
BEGIN
    -- Créer un utilisateur test normal (non-admin)
    DECLARE @testUserId INT;
    EXEC dbo.usp_Utilisateur_Create
        @Pseudo = 'UserTest',
        @Courriel = 'user.test@example.com',
        @MotDePasse = 'test123',
        @UserId = @testUserId OUTPUT;

    PRINT '✓ Utilisateur test créé : user.test@example.com / test123';
END
ELSE
BEGIN
    PRINT '! L''utilisateur test existe déjà';
END
GO

-- =============================================
-- 4. AFFICHER LES UTILISATEURS AVEC LEURS RÔLES
-- =============================================

PRINT '';
PRINT '========================================';
PRINT 'LISTE DES UTILISATEURS ET LEURS RÔLES';
PRINT '========================================';

SELECT
    idUtilisateur_PK AS 'ID',
    pseudo AS 'Pseudo',
    courriel AS 'Email',
    role AS 'Rôle',
    CASE
        WHEN EXISTS (SELECT 1 FROM Blacklist WHERE idUtilisateur = idUtilisateur_PK)
        THEN 'Oui'
        ELSE 'Non'
    END AS 'Blacklisté'
FROM Utilisateur
ORDER BY role DESC, pseudo ASC;

PRINT '';
PRINT '✓ Système de rôles installé avec succès!';
PRINT '';
PRINT 'IMPORTANT:';
PRINT '  - Admin : tokamdaruis@gmail.com (mot de passe existant)';
PRINT '  - User  : user.test@example.com / test123';
GO
