using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;

namespace SallseSense.Services
{
    public class SalleDetailsService
    {
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _dbFactory;

        public SalleDetailsService(IDbContextFactory<Prog3A25BdSalleSenseContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Récupère toutes les données pour la page de détails d'une salle
        /// </summary>
        public async Task<SalleDetailsViewModel?> GetSalleDetailsAsync(int salleId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Charger la salle depuis la BD
            var salleBd = await db.Salles.FindAsync(salleId);
            if (salleBd == null) return null;

            var maintenant = DateTime.Now;
            var aujourdhui = DateTime.Today;
            var demain = aujourdhui.AddDays(1);

            // Vérifier disponibilité
            var estDisponible = !await db.Reservations.AnyAsync(r =>
                r.NoSalle == salleId &&
                r.HeureDebut <= maintenant &&
                r.HeureFin >= maintenant);

            // Charger les réservations du jour
            var resDuJour = await db.Reservations
                .Where(r => r.NoSalle == salleId && r.HeureDebut >= aujourdhui && r.HeureDebut < demain)
                .OrderBy(r => r.HeureDebut)
                .ToListAsync();

            var reservationsDuJour = resDuJour.Select(r => new ReservationViewModel
            {
                HeureDebut = r.HeureDebut,
                HeureFin = r.HeureFin,
                NombrePersonne = r.NombrePersonne,
                EstEnCours = r.HeureDebut <= maintenant && r.HeureFin >= maintenant
            }).ToList();

            // Charger les dernières réservations (activités récentes)
            var dernieresRes = await db.Reservations
                .Where(r => r.NoSalle == salleId)
                .OrderByDescending(r => r.HeureDebut)
                .Take(5)
                .ToListAsync();

            var activitesRecentes = dernieresRes.Select(r => new ActiviteViewModel
            {
                Type = "Réservation",
                Description = $"Réservation pour {r.NombrePersonne} personnes",
                DateHeure = r.HeureDebut
            }).ToList();

            // Charger les photos depuis la table Donnees (photoBlob)
            var photos = await db.Donnees
                .Where(d => d.NoSalle == salleId && d.PhotoBlob != null)
                .OrderByDescending(d => d.DateHeure)
                .Take(10)
                .ToListAsync();

            var photosArchive = photos
                .Where(p => p.PhotoBlob != null && p.PhotoBlob.Length > 0)
                .Select(p => new PhotoViewModel
                {
                    IdDonnee = p.IdDonneePk,
                    DateHeure = p.DateHeure
                }).ToList();

            // Charger tous les capteurs
            var capteursBd = await db.Capteurs.ToListAsync();

            var capteurs = capteursBd.Select(c => new CapteurViewModel
            {
                Nom = c.Nom,
                Type = c.Type,
                EstActif = true,
                DerniereMesure = null
            }).ToList();

            return new SalleDetailsViewModel
            {
                Salle = new SalleInfoViewModel
                {
                    IdSallePk = salleBd.IdSallePk,
                    Numero = salleBd.Numero,
                    CapaciteMaximale = salleBd.CapaciteMaximale,
                    EstDisponible = estDisponible
                },
                ReservationsDuJour = reservationsDuJour,
                ActivitesRecentes = activitesRecentes,
                PhotosArchive = photosArchive,
                Capteurs = capteurs
            };
        }

        /// <summary>
        /// Conteneur principal pour toutes les données de la page
        /// </summary>
        public class SalleDetailsViewModel
        {
            public SalleInfoViewModel Salle { get; set; } = new();
            public List<ReservationViewModel> ReservationsDuJour { get; set; } = new();
            public List<ActiviteViewModel> ActivitesRecentes { get; set; } = new();
            public List<PhotoViewModel> PhotosArchive { get; set; } = new();
            public List<CapteurViewModel> Capteurs { get; set; } = new();
        }

        public class SalleInfoViewModel
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
            public int IdDonnee { get; set; }
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
