using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SallseSense.Data;

namespace SallseSense.Services
{
    /// <summary>
    /// Service d'authentification pour gérer l'inscription et la connexion des utilisateurs.
    /// </summary>
    public class AuthService
    {
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _factory;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IDbContextFactory<Prog3A25BdSalleSenseContext> factory,
            ILogger<AuthService> logger)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Inscrit un nouvel utilisateur via la procédure stockée usp_Utilisateur_Create.
        /// </summary>
        /// <param name="pseudo">Le pseudo de l'utilisateur</param>
        /// <param name="courriel">L'adresse courriel de l'utilisateur</param>
        /// <param name="motDePasse">Le mot de passe en clair (sera hashé par la procédure stockée)</param>
        /// <returns>
        /// Un tuple contenant:
        /// - success: true si l'inscription a réussi, false sinon
        /// - userId: L'ID du nouvel utilisateur si succès, -1 sinon
        /// - message: Un message d'erreur ou de succès
        /// </returns>
        public async Task<(bool success, int userId, string message)> RegisterAsync(string pseudo, string courriel, string motDePasse)
        {
            // Validation des entrées
            if (string.IsNullOrWhiteSpace(pseudo))
                return (false, -1, "Le pseudo est requis.");

            if (string.IsNullOrWhiteSpace(courriel))
                return (false, -1, "Le courriel est requis.");

            if (string.IsNullOrWhiteSpace(motDePasse))
                return (false, -1, "Le mot de passe est requis.");

            if (motDePasse.Length < 6)
                return (false, -1, "Le mot de passe doit contenir au moins 6 caractères.");

            try
            {
                await using var db = await _factory.CreateDbContextAsync();
                var pPseudo = new SqlParameter("@Pseudo", SqlDbType.NVarChar, 100) { Value = pseudo };
                var pCourriel = new SqlParameter("@Courriel", SqlDbType.NVarChar, 255) { Value = courriel };
                var pMdp = new SqlParameter("@MotDePasse", SqlDbType.NVarChar, 4000) { Value = motDePasse };
                var returnValue = new SqlParameter
                {
                    ParameterName = "@UserId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Output
                };

                await db.Database.ExecuteSqlRawAsync(
                    "EXEC[dbo].[usp_Utilisateur_Create] @Pseudo, @Courriel, @MotDePasse, @UserId OUTPUT",
                     pPseudo, pCourriel, pMdp, returnValue);

                int userId = (int)(returnValue.Value ?? -1);

                if (userId > 0)
                {
                    return (true, userId, "Inscription réussie!");
                }
                else if (userId == -1)
                {
                    return (false, -1, "Cette adresse courriel est déjà utilisée.");
                }
                else
                {
                    return (false, -1, "Une erreur est survenue lors de l'inscription.");
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "Erreur SQL lors de l'inscription. Pseudo: {Pseudo}, Courriel: {Courriel}",
                    pseudo, courriel);
                return (false, -1, $"Erreur de base de données: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur inattendue lors de l'inscription. Pseudo: {Pseudo}",
                    pseudo);
                return (false, -1, $"Erreur inattendue: {ex.Message}");
            }
        }

        /// <summary>
        /// Authentifie un utilisateur via la procédure stockée usp_Utilisateur_Login.
        /// </summary>
        /// <param name="courriel">L'adresse courriel de l'utilisateur</param>
        /// <param name="motDePasse">Le mot de passe en clair</param>
        /// <returns>
        /// Un tuple contenant:
        /// - success: true si la connexion a réussi, false sinon
        /// - userId: L'ID de l'utilisateur si succès, -1 sinon
        /// - message: Un message d'erreur ou de succès
        /// </returns>
        public async Task<(bool success, int userId, string message)> LoginAsync(string courriel, string motDePasse)
        {
            // Validation des entrées
            if (string.IsNullOrWhiteSpace(courriel))
                return (false, -1, "Le courriel est requis.");

            if (string.IsNullOrWhiteSpace(motDePasse))
                return (false, -1, "Le mot de passe est requis.");

            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                var pCourriel = new SqlParameter("@Courriel", SqlDbType.NVarChar, 255) { Value = courriel };
                var pMdp = new SqlParameter("@MotDePasse", SqlDbType.NVarChar, 4000) { Value = motDePasse };

                var returnValue = new SqlParameter
                {
                    ParameterName = "@UserId",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Output
                };

                var sql = "EXEC [dbo].[usp_Utilisateur_Login] @Courriel, @MotDePasse, @UserId OUTPUT";

                await db.Database.ExecuteSqlRawAsync(sql, returnValue, pCourriel, pMdp);

                int userId = (int)(returnValue.Value ?? -1);

                if (userId > 0)
                {
                    return (true, userId, "Connexion réussie!");
                }
                else if (userId == -1)
                {
                    return (false, -1, "Courriel ou mot de passe incorrect.");
                }
                else if (userId == -2)
                {
                    return (false, -2, "Votre compte a été bloqué. Contactez l'administrateur.");
                }
                else
                {
                    return (false, -1, "Une erreur est survenue lors de la connexion.");
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "Erreur SQL lors de la connexion. Courriel: {Courriel}",
                    courriel);
                return (false, -1, $"Erreur de base de données: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur inattendue lors de la connexion. Courriel: {Courriel}",
                    courriel);
                return (false, -1, $"Erreur inattendue: {ex.Message}");
            }
        }

        /// <summary>
        /// Vérifie si un utilisateur est dans la blacklist.
        /// </summary>
        /// <param name="userId">L'ID de l'utilisateur</param>
        /// <returns>True si l'utilisateur est blacklisté, false sinon</returns>
        public async Task<bool> IsUserBlacklistedAsync(int userId)
        {
            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                // Vérifier si l'utilisateur existe dans la table Blacklist
                var isBlacklisted = await db.Blacklists
                    .AnyAsync(b => b.IdUtilisateur == userId);

                return isBlacklisted;
            }
            catch (Exception)
            {
                // En cas d'erreur, on suppose que l'utilisateur n'est pas blacklisté

                return false;
            }
        }

        /// <summary>
        /// Récupère les informations d'un utilisateur par son ID.
        /// </summary>
        /// <param name="userId">L'ID de l'utilisateur</param>
        /// <returns>L'utilisateur ou null si non trouvé</returns>
        public async Task<Models.Utilisateur?> GetUserByIdAsync(int userId)
        {
            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                var user = await db.Utilisateurs
                    .FirstOrDefaultAsync(u => u.IdUtilisateurPk == userId);

                return user;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
