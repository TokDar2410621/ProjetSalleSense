using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;
using SallseSense.Models;

namespace SallseSense.Pages
{
    public partial class Dashboard : ComponentBase
    {
        [Inject]
        protected IDbContextFactory<Prog3A25BdSalleSenseContext> DbFactory { get; set; } = default!;

        // Filtres
        protected string filtreType = "all";
        protected string rechercheTexte = string.Empty;
        protected int? capaciteMin = null;
        protected DateTime? dateFiltre = null;
        protected bool vueGrille = true;

        // Statistiques
        protected int nombreSallesActives = 0;
        protected int nombreReservations = 0;
        protected int nombreSallesDisponibles = 0;

        // Listes
        protected List<SalleViewModel> salles = new();
        protected List<ActiviteViewModel> dernieresActivites = new();

        protected DateTime derniereMiseAJour = DateTime.Now;
        protected bool isRefreshing = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadDataFromDatabase();
        }

        protected async Task RafraichirDonnees()
        {
            isRefreshing = true;
            await LoadDataFromDatabase();
            isRefreshing = false;
        }

        protected async Task LoadDataFromDatabase()
        {
            await using var db = await DbFactory.CreateDbContextAsync();

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
            salles = sallesBd.Select(s => new SalleViewModel
            {
                IdSallePk = s.IdSallePk,
                Numero = s.Numero,
                CapaciteMaximale = s.CapaciteMaximale,
                // Vérifier si la salle est disponible (pas de réservation en cours)
                EstDisponible = !reservationsEnCours.Contains(s.IdSallePk),
                MoniteurActif = aCapteurs
            }).ToList();

            // Calculer les statistiques
            nombreSallesActives = sallesBd.Count;
            // Nombre de réservations EN COURS (pas futures)
            nombreReservations = reservationsEnCours.Count;
            nombreSallesDisponibles = salles.Count(s => s.EstDisponible);

            // Charger les dernières activités
            var dernieresRes = await db.Reservations
                .OrderByDescending(r => r.HeureDebut)
                .Take(5)
                .ToListAsync();

            // Créer un dictionnaire des salles pour éviter les requêtes multiples
            var sallesDict = sallesBd.ToDictionary(s => s.IdSallePk, s => s.Numero);

            dernieresActivites = dernieresRes.Select(r => new ActiviteViewModel
            {
                Type = "Réservation",
                NomSalle = sallesDict.ContainsKey(r.NoSalle) ? sallesDict[r.NoSalle] : "Inconnue",
                DateHeure = r.HeureDebut,
                Status = r.HeureFin < maintenant ? "Terminée" : (r.HeureDebut <= maintenant ? "En cours" : "À venir")
            }).ToList();

            // Mettre à jour l'heure de dernière mise à jour
            derniereMiseAJour = DateTime.Now;
        }

        // View Models
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
