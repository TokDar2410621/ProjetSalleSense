using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;

namespace SallseSense.Pages
{
    public partial class SalleDetails : ComponentBase
    {
        [Inject]
        protected IDbContextFactory<Prog3A25BdSalleSenseContext> DbFactory { get; set; } = default!;

        [Parameter]
        public int Id { get; set; }

        private SalleDetailViewModel? salle;
        private List<ReservationViewModel> reservationsDuJour = new();
        private List<ActiviteViewModel> activitesRecentes = new();
        private List<PhotoViewModel> photosArchive = new();
        private List<CapteurViewModel> capteurs = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadDataFromDatabase();
        }

        private async Task LoadDataFromDatabase()
        {
            await using var db = await DbFactory.CreateDbContextAsync();

            // Charger la salle depuis la BD
            var salleBd = await db.Salles.FindAsync(Id);
            if (salleBd == null) return;

            var maintenant = DateTime.Now;
            var aujourdhui = DateTime.Today;
            var demain = aujourdhui.AddDays(1);

            salle = new SalleDetailViewModel
            {
                IdSallePk = salleBd.IdSallePk,
                Numero = salleBd.Numero,
                CapaciteMaximale = salleBd.CapaciteMaximale,
                EstDisponible = !await db.Reservations.AnyAsync(r =>
                    r.NoSalle == Id &&
                    r.HeureDebut <= maintenant &&
                    r.HeureFin >= maintenant)
            };

            // Charger les réservations du jour
            var resDuJour = await db.Reservations
                .Where(r => r.NoSalle == Id && r.HeureDebut >= aujourdhui && r.HeureDebut < demain)
                .OrderBy(r => r.HeureDebut)
                .ToListAsync();

            reservationsDuJour = resDuJour.Select(r => new ReservationViewModel
            {
                HeureDebut = r.HeureDebut,
                HeureFin = r.HeureFin,
                NombrePersonne = r.NombrePersonne,
                EstEnCours = r.HeureDebut <= maintenant && r.HeureFin >= maintenant
            }).ToList();

            // Charger les dernières réservations (activités récentes)
            var dernieresRes = await db.Reservations
                .Where(r => r.NoSalle == Id)
                .OrderByDescending(r => r.HeureDebut)
                .Take(5)
                .ToListAsync();

            activitesRecentes = dernieresRes.Select(r => new ActiviteViewModel
            {
                Type = "Réservation",
                Description = $"Réservation pour {r.NombrePersonne} personnes",
                DateHeure = r.HeureDebut
            }).ToList();

            // Charger les photos depuis la table Donnees (photoBlob)
            var photos = await db.Donnees
                .Where(d => d.NoSalle == Id && d.PhotoBlob != null)
                .OrderByDescending(d => d.DateHeure)
                .Take(10)
                .ToListAsync();

            photosArchive = photos.Select(p => new PhotoViewModel
            {
                DateHeure = p.DateHeure
            }).ToList();

            // Charger tous les capteurs
            var capteursBd = await db.Capteurs.ToListAsync();

            capteurs = capteursBd.Select(c => new CapteurViewModel
            {
                Nom = c.Nom,
                Type = c.Type,
                EstActif = true,
                DerniereMesure = null
            }).ToList();
        }

        public class SalleDetailViewModel
        {
            public int IdSallePk { get; set; }
            public string Numero { get; set; } = string.Empty;
            public int CapaciteMaximale { get; set; }
            public bool EstDisponible { get; set; }
        }

        public class ReservationViewModel
        {
            public DateTime HeureDebut { get; set; }
            public DateTime HeureFin { get; set; }
            public int NombrePersonne { get; set; }
            public bool EstEnCours { get; set; }
        }

        public class ActiviteViewModel
        {
            public string Type { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime DateHeure { get; set; }
        }

        public class PhotoViewModel
        {
            public DateTime DateHeure { get; set; }
        }

        public class CapteurViewModel
        {
            public string Nom { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public bool EstActif { get; set; }
            public double? DerniereMesure { get; set; }
        }
    }
}
