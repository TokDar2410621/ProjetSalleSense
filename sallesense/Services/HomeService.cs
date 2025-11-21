using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;

namespace SallseSense.Services
{
    public class HomeService
    {
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _dbFactory;

        public HomeService(IDbContextFactory<Prog3A25BdSalleSenseContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Récupère les statistiques pour la page d'accueil
        /// </summary>
        public async Task<HomeStatsViewModel> GetStatisticsAsync()
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();

                // Nombre de salles
                var nombreSalles = await db.Salles.CountAsync();

                // Nombre de réservations actives (aujourd'hui ou futures)
                var now = DateTime.Now;
                var nombreReservations = await db.Reservations
                    .Where(r => r.HeureDebut >= now)
                    .CountAsync();

                // Nombre d'utilisateurs actifs (non blacklistés)
                var blacklistedUserIds = await db.Blacklists
                    .Select(b => b.IdUtilisateur)
                    .ToListAsync();

                var nombreUtilisateurs = await db.Utilisateurs
                    .Where(u => !blacklistedUserIds.Contains(u.IdUtilisateurPk))
                    .CountAsync();

                // Nombre de photos capturées
                var nombrePhotos = await db.Donnees
                    .Where(d => d.PhotoBlob != null)
                    .CountAsync();

                return new HomeStatsViewModel
                {
                    NombreSalles = nombreSalles,
                    NombreReservations = nombreReservations,
                    NombreUtilisateurs = nombreUtilisateurs,
                    NombrePhotos = nombrePhotos
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des statistiques: {ex.Message}");

                // Valeurs par défaut en cas d'erreur
                return new HomeStatsViewModel
                {
                    NombreSalles = 0,
                    NombreReservations = 0,
                    NombreUtilisateurs = 0,
                    NombrePhotos = 0
                };
            }
        }

        /// <summary>
        /// ViewModel pour les statistiques de la page d'accueil
        /// </summary>
        public class HomeStatsViewModel
        {
            public int NombreSalles { get; set; }
            public int NombreReservations { get; set; }
            public int NombreUtilisateurs { get; set; }
            public int NombrePhotos { get; set; }
        }
    }
}
