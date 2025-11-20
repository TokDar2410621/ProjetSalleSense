using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;

namespace SallseSense.Pages
{
    public partial class Index : ComponentBase
    {
        [Inject]
        protected IDbContextFactory<Prog3A25BdSalleSenseContext> DbFactory { get; set; } = default!;

        protected int nombreSalles = 0;
        protected int nombreReservations = 0;
        protected int nombreUtilisateurs = 0;
        protected int nombrePhotos = 0;

        protected override async Task OnInitializedAsync()
        {
            await LoadStatistics();
        }

        protected async Task LoadStatistics()
        {
            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();

                // Nombre de salles
                nombreSalles = await db.Salles.CountAsync();

                // Nombre de réservations actives (aujourd'hui ou futures)
                var now = DateTime.Now;
                nombreReservations = await db.Reservations
                    .Where(r => r.HeureDebut >= now)
                    .CountAsync();

                // Nombre d'utilisateurs actifs (non blacklistés)
                var blacklistedUserIds = await db.Blacklists
                    .Select(b => b.IdUtilisateur)
                    .ToListAsync();

                nombreUtilisateurs = await db.Utilisateurs
                    .Where(u => !blacklistedUserIds.Contains(u.IdUtilisateurPk))
                    .CountAsync();

                // Nombre de photos capturées
                nombrePhotos = await db.Donnees
                    .Where(d => d.PhotoBlob != null)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des statistiques: {ex.Message}");
                // Valeurs par défaut en cas d'erreur
                nombreSalles = 0;
                nombreReservations = 0;
                nombreUtilisateurs = 0;
                nombrePhotos = 0;
            }
        }
    }
}