using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;
using SallseSense.Models;

namespace SallseSense.Services
{
    public class AdminService
    {
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _dbFactory;

        public AdminService(IDbContextFactory<Prog3A25BdSalleSenseContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Récupère tous les utilisateurs avec leur statut blacklist
        /// </summary>
        public async Task<AdminViewModel> GetAdminDataAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var utilisateurs = await db.Utilisateurs
                .Select(u => new UtilisateurViewModel
                {
                    IdUtilisateurPk = u.IdUtilisateurPk,
                    Pseudo = u.Pseudo,
                    Courriel = u.Courriel,
                    Role = u.Role
                })
                .ToListAsync();

            var blacklistedUserIds = await db.Blacklists
                .Select(b => b.IdUtilisateur)
                .ToListAsync();

            return new AdminViewModel
            {
                Utilisateurs = utilisateurs,
                BlacklistedUserIds = blacklistedUserIds
            };
        }

        /// <summary>
        /// Vérifie si un utilisateur est administrateur
        /// </summary>
        public async Task<bool> IsAdminAsync(int userId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var user = await db.Utilisateurs.FindAsync(userId);
            return user?.Role == "Admin";
        }

        /// <summary>
        /// Ajoute un utilisateur à la blacklist
        /// </summary>
        public async Task<OperationResult> AddToBlacklistAsync(int userId, int adminId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Vérifier que l'utilisateur existe
            var user = await db.Utilisateurs.FindAsync(userId);
            if (user == null)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "Utilisateur introuvable."
                };
            }

            // Vérifier que l'utilisateur n'est pas déjà blacklisté
            var existingBlacklist = await db.Blacklists
                .FirstOrDefaultAsync(b => b.IdUtilisateur == userId);

            if (existingBlacklist != null)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "Cet utilisateur est déjà blacklisté."
                };
            }

            // Ajouter à la blacklist
            var blacklist = new Blacklist
            {
                IdUtilisateur = userId
            };

            db.Blacklists.Add(blacklist);
            await db.SaveChangesAsync();

            return new OperationResult
            {
                Success = true,
                Message = $"L'utilisateur {user.Pseudo} a été blacklisté avec succès."
            };
        }

        /// <summary>
        /// Retire un utilisateur de la blacklist
        /// </summary>
        public async Task<OperationResult> RemoveFromBlacklistAsync(int userId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var blacklist = await db.Blacklists
                .FirstOrDefaultAsync(b => b.IdUtilisateur == userId);

            if (blacklist == null)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "Cet utilisateur n'est pas blacklisté."
                };
            }

            var user = await db.Utilisateurs.FindAsync(userId);

            db.Blacklists.Remove(blacklist);
            await db.SaveChangesAsync();

            return new OperationResult
            {
                Success = true,
                Message = $"L'utilisateur {user?.Pseudo} a été débloqué avec succès."
            };
        }

        /// <summary>
        /// ViewModel pour la page admin
        /// </summary>
        public class AdminViewModel
        {
            public List<UtilisateurViewModel> Utilisateurs { get; set; } = new();
            public List<int> BlacklistedUserIds { get; set; } = new();
        }

        /// <summary>
        /// ViewModel pour un utilisateur
        /// </summary>
        public class UtilisateurViewModel
        {
            public int IdUtilisateurPk { get; set; }
            public string Pseudo { get; set; } = string.Empty;
            public string Courriel { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        /// <summary>
        /// Résultat d'une opération
        /// </summary>
        public class OperationResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }
}
