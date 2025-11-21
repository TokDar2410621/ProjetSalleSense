using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;

namespace SallseSense.Services
{
    public class DashboardService
    {
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _dbFactory;

        public DashboardService(IDbContextFactory<Prog3A25BdSalleSenseContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Récupère toutes les données pour le dashboard
        /// </summary>
        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Charger toutes les salles depuis la BD
            var sallesBd = await db.Salles.ToListAsync();
            var maintenant = DateTime.Now;

            // Charger toutes les réservations en cours
            var reservationsEnCours = await db.Reservations
                .Where(r => r.HeureDebut <= maintenant && r.HeureFin >= maintenant)
                .Select(r => r.NoSalle)
                .ToListAsync();

            // Vérifier s'il y a des capteurs
            var aCapteurs = await db.Capteurs.AnyAsync();

            // Construire les ViewModels des salles
            var salles = sallesBd.Select(s => new SalleViewModel
            {
                IdSallePk = s.IdSallePk,
                Numero = s.Numero,
                CapaciteMaximale = s.CapaciteMaximale,
                // Vérifier si la salle est disponible (pas de réservation en cours)
                EstDisponible = !reservationsEnCours.Contains(s.IdSallePk),
                MoniteurActif = aCapteurs
            }).ToList();

            // Calculer les statistiques
            var nombreSallesActives = sallesBd.Count;
            var nombreReservations = reservationsEnCours.Count;
            var nombreSallesDisponibles = salles.Count(s => s.EstDisponible);

            // Charger les dernières activités
            var dernieresRes = await db.Reservations
                .OrderByDescending(r => r.HeureDebut)
                .Take(5)
                .ToListAsync();

            // Créer un dictionnaire des salles pour éviter les requêtes multiples
            var sallesDict = sallesBd.ToDictionary(s => s.IdSallePk, s => s.Numero);

            var dernieresActivites = dernieresRes.Select(r => new ActiviteViewModel
            {
                Type = "Réservation",
                NomSalle = sallesDict.ContainsKey(r.NoSalle) ? sallesDict[r.NoSalle] : "Inconnue",
                DateHeure = r.HeureDebut,
                Status = r.HeureFin < maintenant ? "Terminée" : (r.HeureDebut <= maintenant ? "En cours" : "À venir")
            }).ToList();

            return new DashboardViewModel
            {
                Salles = salles,
                NombreSallesActives = nombreSallesActives,
                NombreReservations = nombreReservations,
                NombreSallesDisponibles = nombreSallesDisponibles,
                DernieresActivites = dernieresActivites,
                DerniereMiseAJour = DateTime.Now
            };
        }

        /// <summary>
        /// ViewModel conteneur pour toutes les données du dashboard
        /// </summary>
        public class DashboardViewModel
        {
            public List<SalleViewModel> Salles { get; set; } = new();
            public int NombreSallesActives { get; set; }
            public int NombreReservations { get; set; }
            public int NombreSallesDisponibles { get; set; }
            public List<ActiviteViewModel> DernieresActivites { get; set; } = new();
            public DateTime DerniereMiseAJour { get; set; }
        }

        public class SalleViewModel
        {
            public int IdSallePk { get; set; }
            public string Numero { get; set; } = string.Empty;
            public int CapaciteMaximale { get; set; }
            public bool EstDisponible { get; set; }
            public bool MoniteurActif { get; set; }
        }

        public class ActiviteViewModel
        {
            public string Type { get; set; } = string.Empty;
            public string NomSalle { get; set; } = string.Empty;
            public DateTime DateHeure { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}
