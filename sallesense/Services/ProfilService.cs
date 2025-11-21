using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SallseSense.Data;
using SallseSense.Models;

namespace SallseSense.Services
{
    public class ProfilService
    {
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _dbFactory;

        public ProfilService(IDbContextFactory<Prog3A25BdSalleSenseContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Charge les données de profil d'un utilisateur
        /// </summary>
        public async Task<Utilisateur?> GetUtilisateurAsync(int userId)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();
                return await db.Utilisateurs
                    .FirstOrDefaultAsync(u => u.IdUtilisateurPk == userId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Met à jour les informations du profil (appelle la procédure stockée)
        /// </summary>
        public async Task<ModificationProfilResult> ModifierProfilAsync(int userId, string nouveauPseudo, string nouveauCourriel)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();

                var pIdUtilisateur = new SqlParameter("@IdUtilisateur", System.Data.SqlDbType.Int) { Value = userId };
                var pNouveauPseudo = new SqlParameter("@NouveauPseudo", System.Data.SqlDbType.NVarChar, 40) { Value = nouveauPseudo };
                var pNouveauCourriel = new SqlParameter("@NouveauCourriel", System.Data.SqlDbType.NVarChar, 40) { Value = nouveauCourriel };
                var pCodeStatut = new SqlParameter("@CodeStatut", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };

                await db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.usp_Utilisateur_Modifier @IdUtilisateur, @NouveauPseudo, @NouveauCourriel, @CodeStatut OUTPUT",
                    pIdUtilisateur, pNouveauPseudo, pNouveauCourriel, pCodeStatut);

                int codeStatut = pCodeStatut.Value != DBNull.Value ? (int)pCodeStatut.Value : -99;

                return codeStatut switch
                {
                    0 => new ModificationProfilResult { Success = true, Message = "Vos informations ont été mises à jour avec succès!" },
                    -1 => new ModificationProfilResult { Success = false, Message = "Utilisateur introuvable." },
                    -2 => new ModificationProfilResult { Success = false, Message = "Ce pseudo est déjà utilisé par un autre utilisateur." },
                    -3 => new ModificationProfilResult { Success = false, Message = "Cet email est déjà utilisé par un autre utilisateur." },
                    -99 => new ModificationProfilResult { Success = false, Message = "Erreur système lors de la modification." },
                    _ => new ModificationProfilResult { Success = false, Message = "Erreur inconnue." }
                };
            }
            catch (Exception ex)
            {
                return new ModificationProfilResult { Success = false, Message = $"Erreur lors de la sauvegarde: {ex.Message}" };
            }
        }

        /// <summary>
        /// Change le mot de passe (appelle la procédure stockée avec hashing)
        /// </summary>
        public async Task<ChangementMotDePasseResult> ChangerMotDePasseAsync(
            int userId,
            string ancienMotDePasse,
            string nouveauMotDePasse,
            string confirmationMotDePasse)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(ancienMotDePasse))
                    return new ChangementMotDePasseResult { Success = false, Message = "Veuillez entrer votre ancien mot de passe." };

                if (string.IsNullOrWhiteSpace(nouveauMotDePasse))
                    return new ChangementMotDePasseResult { Success = false, Message = "Veuillez entrer un nouveau mot de passe." };

                if (nouveauMotDePasse != confirmationMotDePasse)
                    return new ChangementMotDePasseResult { Success = false, Message = "Les mots de passe ne correspondent pas." };

                if (nouveauMotDePasse.Length < 6)
                    return new ChangementMotDePasseResult { Success = false, Message = "Le mot de passe doit contenir au moins 6 caractères." };

                await using var db = await _dbFactory.CreateDbContextAsync();

                // Générer nouveau salt
                var nouveauSaltBytes = new byte[16];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(nouveauSaltBytes);
                }

                // Calculer le hash (en simulant le comportement de la procédure stockée)
                var saltHex = BitConverter.ToString(nouveauSaltBytes).Replace("-", "");
                var nouveauHashBytes = System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(saltHex + nouveauMotDePasse)
                );

                // Paramètres pour la procédure stockée
                var pIdUtilisateur = new SqlParameter("@IdUtilisateur", System.Data.SqlDbType.Int) { Value = userId };
                var pAncienMotDePasse = new SqlParameter("@AncienMotDePasse", System.Data.SqlDbType.NVarChar, -1) { Value = ancienMotDePasse };
                var pNouveauSalt = new SqlParameter("@NouveauSalt", System.Data.SqlDbType.VarBinary, 16) { Value = nouveauSaltBytes };
                var pNouveauHash = new SqlParameter("@NouveauHash", System.Data.SqlDbType.VarBinary, 32) { Value = nouveauHashBytes };
                var pCodeStatut = new SqlParameter("@CodeStatut", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };

                await db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.usp_Utilisateur_ChangerMotDePasse @IdUtilisateur, @AncienMotDePasse, @NouveauSalt, @NouveauHash, @CodeStatut OUTPUT",
                    pIdUtilisateur, pAncienMotDePasse, pNouveauSalt, pNouveauHash, pCodeStatut);

                int codeStatut = pCodeStatut.Value != DBNull.Value ? (int)pCodeStatut.Value : -99;

                return codeStatut switch
                {
                    0 => new ChangementMotDePasseResult { Success = true, Message = "Votre mot de passe a été changé avec succès!" },
                    -1 => new ChangementMotDePasseResult { Success = false, Message = "Utilisateur introuvable." },
                    -2 => new ChangementMotDePasseResult { Success = false, Message = "L'ancien mot de passe est incorrect." },
                    -99 => new ChangementMotDePasseResult { Success = false, Message = "Erreur système lors du changement de mot de passe." },
                    _ => new ChangementMotDePasseResult { Success = false, Message = "Erreur inconnue." }
                };
            }
            catch (Exception ex)
            {
                return new ChangementMotDePasseResult { Success = false, Message = $"Erreur lors du changement de mot de passe: {ex.Message}" };
            }
        }

        /// <summary>
        /// Résultat d'une modification de profil
        /// </summary>
        public class ModificationProfilResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        /// <summary>
        /// Résultat d'un changement de mot de passe
        /// </summary>
        public class ChangementMotDePasseResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }
}
