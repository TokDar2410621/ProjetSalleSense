using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SallseSense.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SallseSense.Services
{
    /// <summary>
    /// Service pour les statistiques des salles
    /// </summary>
    public class SalleStatistiquesService
    {
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _contextFactory;
        private readonly ILogger<SalleStatistiquesService> _logger;

        public SalleStatistiquesService(
            IDbContextFactory<Prog3A25BdSalleSenseContext> contextFactory,
            ILogger<SalleStatistiquesService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Récupère toutes les statistiques d'une salle
        /// </summary>
        public async Task<SalleStatistiquesViewModel> GetStatistiquesAsync(int salleId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var salle = await context.Salles.FindAsync(salleId);
                if (salle == null)
                    return null;

                var now = DateTime.Now;
                var debutMois = new DateTime(now.Year, now.Month, 1);
                var debutSemaine = now.AddDays(-(int)now.DayOfWeek);
                var debut30Jours = now.AddDays(-30);
                var debut7Jours = now.AddDays(-7);

                // Récupérer toutes les réservations de la salle
                var reservations = await context.Reservations
                    .Where(r => r.NoSalle == salleId)
                    .ToListAsync();

                // Statistiques générales
                var totalReservations = reservations.Count;
                var reservationsCeMois = reservations.Count(r => r.HeureDebut >= debutMois);
                var reservationsCetteSemaine = reservations.Count(r => r.HeureDebut >= debutSemaine);
                var reservationsEnCours = reservations.Count(r => r.HeureDebut <= now && r.HeureFin >= now);

                // Durée moyenne des réservations (en minutes)
                var dureeMoyenne = reservations.Any()
                    ? reservations.Average(r => (r.HeureFin - r.HeureDebut).TotalMinutes)
                    : 0;

                // Nombre moyen de personnes
                var personnesMoyenne = reservations.Any()
                    ? reservations.Average(r => r.NombrePersonne)
                    : 0;

                // Taux d'occupation (heures réservées / heures ouvrables sur 30 jours)
                var heuresReservees30j = reservations
                    .Where(r => r.HeureDebut >= debut30Jours)
                    .Sum(r => (r.HeureFin - r.HeureDebut).TotalHours);
                var heuresOuvrables30j = 30 * 10; // 10 heures par jour
                var tauxOccupation = heuresOuvrables30j > 0
                    ? Math.Min(100, (heuresReservees30j / heuresOuvrables30j) * 100)
                    : 0;

                // Réservations par jour de la semaine
                var parJourSemaine = reservations
                    .GroupBy(r => r.HeureDebut.DayOfWeek)
                    .Select(g => new JourSemaineStats
                    {
                        Jour = GetNomJour(g.Key),
                        JourIndex = (int)g.Key,
                        NombreReservations = g.Count()
                    })
                    .OrderBy(j => j.JourIndex)
                    .ToList();

                // Compléter les jours manquants
                for (int i = 0; i < 7; i++)
                {
                    if (!parJourSemaine.Any(j => j.JourIndex == i))
                    {
                        parJourSemaine.Add(new JourSemaineStats
                        {
                            Jour = GetNomJour((DayOfWeek)i),
                            JourIndex = i,
                            NombreReservations = 0
                        });
                    }
                }
                parJourSemaine = parJourSemaine.OrderBy(j => j.JourIndex).ToList();

                // Réservations par heure de la journée
                var parHeure = reservations
                    .GroupBy(r => r.HeureDebut.Hour)
                    .Select(g => new HeureStats
                    {
                        Heure = g.Key,
                        HeureFormatee = $"{g.Key}h",
                        NombreReservations = g.Count()
                    })
                    .OrderBy(h => h.Heure)
                    .ToList();

                // Compléter les heures manquantes (8h à 20h)
                for (int h = 8; h <= 20; h++)
                {
                    if (!parHeure.Any(x => x.Heure == h))
                    {
                        parHeure.Add(new HeureStats
                        {
                            Heure = h,
                            HeureFormatee = $"{h}h",
                            NombreReservations = 0
                        });
                    }
                }
                parHeure = parHeure.Where(h => h.Heure >= 8 && h.Heure <= 20).OrderBy(h => h.Heure).ToList();

                // Évolution sur les 30 derniers jours
                var evolution30Jours = new List<EvolutionJourStats>();
                for (int i = 29; i >= 0; i--)
                {
                    var jour = now.AddDays(-i).Date;
                    var count = reservations.Count(r => r.HeureDebut.Date == jour);
                    evolution30Jours.Add(new EvolutionJourStats
                    {
                        Date = jour,
                        DateFormatee = jour.ToString("dd/MM"),
                        NombreReservations = count
                    });
                }

                // Statistiques des capteurs
                var donneesCapteurs = await context.Donnees
                    .Where(d => d.NoSalle == salleId && d.DateHeure >= debut7Jours)
                    .ToListAsync();

                var nombrePhotos = donneesCapteurs.Count(d => d.PhotoBlob != null && d.PhotoBlob.Length > 0);
                var nombreMesures = donneesCapteurs.Count(d => d.Mesure.HasValue);

                // Dernières réservations
                var dernieresReservations = reservations
                    .OrderByDescending(r => r.HeureDebut)
                    .Take(5)
                    .Select(r => new ReservationResume
                    {
                        Id = r.IdReservationPk,
                        HeureDebut = r.HeureDebut,
                        HeureFin = r.HeureFin,
                        NombrePersonnes = r.NombrePersonne,
                        EstPassee = r.HeureFin < now,
                        EstEnCours = r.HeureDebut <= now && r.HeureFin >= now
                    })
                    .ToList();

                // Événements de la salle (via Donnees)
                var evenements = await context.Evenements
                    .Join(context.Donnees,
                        e => e.IdDonnee,
                        d => d.IdDonneePk,
                        (e, d) => new { Evenement = e, Donnee = d })
                    .Where(ed => ed.Donnee.NoSalle == salleId)
                    .OrderByDescending(ed => ed.Donnee.DateHeure)
                    .Take(20)
                    .Select(ed => new EvenementResume
                    {
                        Id = ed.Evenement.IdEvenementPk,
                        Type = ed.Evenement.Type,
                        Description = ed.Evenement.Description ?? "",
                        DateHeure = ed.Donnee.DateHeure,
                        IdCapteur = ed.Donnee.IdCapteur
                    })
                    .ToListAsync();

                // Compter les événements des 7 derniers jours
                var nombreEvenements7Jours = await context.Evenements
                    .Join(context.Donnees,
                        e => e.IdDonnee,
                        d => d.IdDonneePk,
                        (e, d) => new { Evenement = e, Donnee = d })
                    .Where(ed => ed.Donnee.NoSalle == salleId && ed.Donnee.DateHeure >= debut7Jours)
                    .CountAsync();

                // Données sonores (capteurs de type BRUIT)
                var mesuresSonores = await context.Donnees
                    .Join(context.Capteurs,
                        d => d.IdCapteur,
                        c => c.IdCapteurPk,
                        (d, c) => new { Donnee = d, Capteur = c })
                    .Where(dc => dc.Donnee.NoSalle == salleId
                              && dc.Capteur.Type == "BRUIT"
                              && dc.Donnee.Mesure.HasValue
                              && dc.Donnee.DateHeure >= debut7Jours)
                    .OrderByDescending(dc => dc.Donnee.DateHeure)
                    .Take(50)
                    .Select(dc => new MesureSonore
                    {
                        DateHeure = dc.Donnee.DateHeure,
                        NiveauDb = dc.Donnee.Mesure.Value,
                        DateFormatee = dc.Donnee.DateHeure.ToString("dd/MM"),
                        HeureFormatee = dc.Donnee.DateHeure.ToString("HH:mm")
                    })
                    .ToListAsync();

                // Statistiques sonores
                var niveauSonoreMoyen = mesuresSonores.Any() ? mesuresSonores.Average(m => m.NiveauDb) : 0;
                var niveauSonoreMax = mesuresSonores.Any() ? mesuresSonores.Max(m => m.NiveauDb) : 0;
                var niveauSonoreMin = mesuresSonores.Any() ? mesuresSonores.Min(m => m.NiveauDb) : 0;

                return new SalleStatistiquesViewModel
                {
                    SalleId = salleId,
                    NumeroSalle = salle.Numero,
                    CapaciteMaximale = salle.CapaciteMaximale,

                    // Stats générales
                    TotalReservations = totalReservations,
                    ReservationsCeMois = reservationsCeMois,
                    ReservationsCetteSemaine = reservationsCetteSemaine,
                    ReservationsEnCours = reservationsEnCours,
                    DureeMoyenneMinutes = (int)dureeMoyenne,
                    PersonnesMoyenne = Math.Round(personnesMoyenne, 1),
                    TauxOccupation = Math.Round(tauxOccupation, 1),

                    // Données capteurs
                    NombrePhotos7Jours = nombrePhotos,
                    NombreMesures7Jours = nombreMesures,

                    // Graphiques
                    ReservationsParJour = parJourSemaine,
                    ReservationsParHeure = parHeure,
                    Evolution30Jours = evolution30Jours,

                    // Dernières réservations
                    DernieresReservations = dernieresReservations,

                    // Événements
                    Evenements = evenements,
                    NombreEvenements7Jours = nombreEvenements7Jours,

                    // Données sonores
                    MesuresSonores = mesuresSonores.OrderBy(m => m.DateHeure).ToList(),
                    NiveauSonoreMoyen = Math.Round(niveauSonoreMoyen, 1),
                    NiveauSonoreMax = Math.Round(niveauSonoreMax, 1),
                    NiveauSonoreMin = Math.Round(niveauSonoreMin, 1)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des statistiques pour la salle {salleId}");
                return null;
            }
        }

        private string GetNomJour(DayOfWeek jour)
        {
            return jour switch
            {
                DayOfWeek.Monday => "Lun",
                DayOfWeek.Tuesday => "Mar",
                DayOfWeek.Wednesday => "Mer",
                DayOfWeek.Thursday => "Jeu",
                DayOfWeek.Friday => "Ven",
                DayOfWeek.Saturday => "Sam",
                DayOfWeek.Sunday => "Dim",
                _ => ""
            };
        }

        #region ViewModels

        public class SalleStatistiquesViewModel
        {
            public int SalleId { get; set; }
            public string NumeroSalle { get; set; }
            public int CapaciteMaximale { get; set; }

            // Statistiques générales
            public int TotalReservations { get; set; }
            public int ReservationsCeMois { get; set; }
            public int ReservationsCetteSemaine { get; set; }
            public int ReservationsEnCours { get; set; }
            public int DureeMoyenneMinutes { get; set; }
            public double PersonnesMoyenne { get; set; }
            public double TauxOccupation { get; set; }

            // Capteurs
            public int NombrePhotos7Jours { get; set; }
            public int NombreMesures7Jours { get; set; }

            // Données pour graphiques
            public List<JourSemaineStats> ReservationsParJour { get; set; } = new();
            public List<HeureStats> ReservationsParHeure { get; set; } = new();
            public List<EvolutionJourStats> Evolution30Jours { get; set; } = new();

            // Dernières réservations
            public List<ReservationResume> DernieresReservations { get; set; } = new();

            // Événements
            public List<EvenementResume> Evenements { get; set; } = new();
            public int NombreEvenements7Jours { get; set; }

            // Données sonores
            public List<MesureSonore> MesuresSonores { get; set; } = new();
            public double NiveauSonoreMoyen { get; set; }
            public double NiveauSonoreMax { get; set; }
            public double NiveauSonoreMin { get; set; }

            // Propriétés calculées
            public string DureeMoyenneFormatee => DureeMoyenneMinutes >= 60
                ? $"{DureeMoyenneMinutes / 60}h {DureeMoyenneMinutes % 60}min"
                : $"{DureeMoyenneMinutes} min";

            public int MaxReservationsParJour => ReservationsParJour.Any()
                ? ReservationsParJour.Max(j => j.NombreReservations)
                : 1;

            public int MaxReservationsParHeure => ReservationsParHeure.Any()
                ? ReservationsParHeure.Max(h => h.NombreReservations)
                : 1;

            public int MaxEvolution => Evolution30Jours.Any()
                ? Math.Max(1, Evolution30Jours.Max(e => e.NombreReservations))
                : 1;
        }

        public class JourSemaineStats
        {
            public string Jour { get; set; }
            public int JourIndex { get; set; }
            public int NombreReservations { get; set; }
        }

        public class HeureStats
        {
            public int Heure { get; set; }
            public string HeureFormatee { get; set; }
            public int NombreReservations { get; set; }
        }

        public class EvolutionJourStats
        {
            public DateTime Date { get; set; }
            public string DateFormatee { get; set; }
            public int NombreReservations { get; set; }
        }

        public class ReservationResume
        {
            public int Id { get; set; }
            public DateTime HeureDebut { get; set; }
            public DateTime HeureFin { get; set; }
            public int NombrePersonnes { get; set; }
            public bool EstPassee { get; set; }
            public bool EstEnCours { get; set; }
        }

        public class EvenementResume
        {
            public int Id { get; set; }
            public string Type { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime DateHeure { get; set; }
            public int? IdCapteur { get; set; }
            public string NomCapteur { get; set; } = string.Empty;
        }

        public class MesureSonore
        {
            public DateTime DateHeure { get; set; }
            public double NiveauDb { get; set; }
            public string DateFormatee { get; set; } = string.Empty;
            public string HeureFormatee { get; set; } = string.Empty;

            // Catégorie de niveau sonore
            public string Categorie => NiveauDb switch
            {
                < 40 => "Silencieux",
                < 60 => "Normal",
                < 75 => "Modéré",
                < 85 => "Fort",
                _ => "Très fort"
            };

            public string CouleurClasse => NiveauDb switch
            {
                < 40 => "sound-level-low",
                < 60 => "sound-level-normal",
                < 75 => "sound-level-moderate",
                < 85 => "sound-level-high",
                _ => "sound-level-very-high"
            };
        }

        #endregion
    }
}
