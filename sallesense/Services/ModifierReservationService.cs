using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;
using SallseSense.Models;

namespace SallseSense.Services
{
    public class ModifierReservationService
    {
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _dbFactory;
        private readonly ReservationService _reservationService;

        public ModifierReservationService(
            IDbContextFactory<Prog3A25BdSalleSenseContext> dbFactory,
            ReservationService reservationService)
        {
            _dbFactory = dbFactory;
            _reservationService = reservationService;
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
        /// Charge les données pour modifier une réservation
        /// </summary>
        public async Task<ModifierReservationViewModel?> GetReservationDataAsync(int reservationId, int userId, bool isAdmin = false)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var reservation = await db.Reservations
                .FirstOrDefaultAsync(r => r.IdReservationPk == reservationId);

            if (reservation == null)
            {
                return null;
            }

            // Charger la salle séparément
            var salle = await db.Salles.FindAsync(reservation.NoSalle);
            if (salle == null)
            {
                return null;
            }

            // Charger toutes les réservations de l'utilisateur pour le même salle
            // Si admin, charger toutes les réservations de la salle
            var toutesReservations = await db.Reservations
                .Where(r => (isAdmin || r.NoPersonne == userId) && r.NoSalle == reservation.NoSalle)
                .OrderByDescending(r => r.HeureDebut)
                .Select(r => new ReservationSimpleViewModel
                {
                    IdReservationPk = r.IdReservationPk,
                    HeureDebut = r.HeureDebut,
                    HeureFin = r.HeureFin,
                    NombrePersonne = r.NombrePersonne
                })
                .ToListAsync();

            // Charger les autres réservations pour la même salle (pour éviter les conflits)
            var autresReservations = await db.Reservations
                .Where(r => r.NoSalle == reservation.NoSalle && r.IdReservationPk != reservationId)
                .Select(r => new ReservationSimpleViewModel
                {
                    IdReservationPk = r.IdReservationPk,
                    HeureDebut = r.HeureDebut,
                    HeureFin = r.HeureFin,
                    NombrePersonne = r.NombrePersonne
                })
                .ToListAsync();

            return new ModifierReservationViewModel
            {
                Reservation = reservation,
                SalleInfo = new SalleInfoViewModel
                {
                    IdSallePk = salle.IdSallePk,
                    Numero = salle.Numero,
                    CapaciteMaximale = salle.CapaciteMaximale
                },
                ToutesReservations = toutesReservations,
                AutresReservations = autresReservations
            };
        }

        /// <summary>
        /// Modifie une réservation existante via la procédure stockée
        /// </summary>
        public async Task<ModificationResult> ModifierReservationAsync(
            int reservationId,
            int userId,
            DateTime nouvelleHeureDebut,
            DateTime nouvelleHeureFin,
            int nouveauNombrePersonne,
            bool isAdmin = false)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            // Validation des heures
            if (nouvelleHeureFin <= nouvelleHeureDebut)
            {
                return new ModificationResult
                {
                    Success = false,
                    Message = "L'heure de fin doit être après l'heure de début."
                };
            }

            // Appeler la procédure stockée
            var pIdReservation = new SqlParameter("@IdReservation", SqlDbType.Int) { Value = reservationId };
            var pIdUtilisateur = new SqlParameter("@IdUtilisateur", SqlDbType.Int) { Value = userId };
            var pEstAdmin = new SqlParameter("@EstAdmin", SqlDbType.Bit) { Value = isAdmin };
            var pNouvelleHeureDebut = new SqlParameter("@NouvelleHeureDebut", SqlDbType.DateTime2) { Value = nouvelleHeureDebut };
            var pNouvelleHeureFin = new SqlParameter("@NouvelleHeureFin", SqlDbType.DateTime2) { Value = nouvelleHeureFin };
            var pNouveauNombrePersonnes = new SqlParameter("@NouveauNombrePersonnes", SqlDbType.Int) { Value = nouveauNombrePersonne };
            var pCodeStatut = new SqlParameter("@CodeStatut", SqlDbType.Int) { Direction = ParameterDirection.Output };

            await db.Database.ExecuteSqlRawAsync(
                "EXEC dbo.usp_Reservation_Modifier @IdReservation, @IdUtilisateur, @EstAdmin, @NouvelleHeureDebut, @NouvelleHeureFin, @NouveauNombrePersonnes, @CodeStatut OUTPUT",
                pIdReservation, pIdUtilisateur, pEstAdmin, pNouvelleHeureDebut, pNouvelleHeureFin, pNouveauNombrePersonnes, pCodeStatut);

            int codeStatut = pCodeStatut.Value != DBNull.Value ? (int)pCodeStatut.Value : -99;

            return codeStatut switch
            {
                0 => new ModificationResult { Success = true, Message = "Réservation modifiée avec succès." },
                -1 => new ModificationResult { Success = false, Message = "Réservation introuvable." },
                -2 => new ModificationResult { Success = false, Message = "Vous n'avez pas les droits pour modifier cette réservation." },
                -3 => new ModificationResult { Success = false, Message = "Salle introuvable." },
                -4 => new ModificationResult { Success = false, Message = "Le nombre de personnes dépasse la capacité de la salle." },
                -5 => new ModificationResult { Success = false, Message = "La salle est déjà réservée pour ce créneau." },
                _ => new ModificationResult { Success = false, Message = "Une erreur s'est produite lors de la modification." }
            };
        }

        /// <summary>
        /// Annule une réservation (admin peut annuler n'importe quelle réservation)
        /// </summary>
        public async Task<ModificationResult> AnnulerReservationAsync(int reservationId, int userId, bool isAdmin = false)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var reservation = await db.Reservations
                .FirstOrDefaultAsync(r => r.IdReservationPk == reservationId);

            if (reservation == null)
            {
                return new ModificationResult
                {
                    Success = false,
                    Message = "Réservation introuvable."
                };
            }

            // Vérifier les permissions (admin ou propriétaire)
            if (!isAdmin && reservation.NoPersonne != userId)
            {
                return new ModificationResult
                {
                    Success = false,
                    Message = "Vous n'avez pas les droits pour annuler cette réservation."
                };
            }

            db.Reservations.Remove(reservation);
            await db.SaveChangesAsync();

            return new ModificationResult
            {
                Success = true,
                Message = "Réservation annulée avec succès."
            };
        }

        /// <summary>
        /// ViewModel pour la page de modification
        /// </summary>
        public class ModifierReservationViewModel
        {
            public Reservation Reservation { get; set; } = null!;
            public SalleInfoViewModel SalleInfo { get; set; } = null!;
            public List<ReservationSimpleViewModel> ToutesReservations { get; set; } = new();
            public List<ReservationSimpleViewModel> AutresReservations { get; set; } = new();
        }

        /// <summary>
        /// ViewModel pour les informations de salle
        /// </summary>
        public class SalleInfoViewModel
        {
            public int IdSallePk { get; set; }
            public string Numero { get; set; } = string.Empty;
            public int CapaciteMaximale { get; set; }
        }

        /// <summary>
        /// ViewModel simplifié pour une réservation
        /// </summary>
        public class ReservationSimpleViewModel
        {
            public int IdReservationPk { get; set; }
            public DateTime HeureDebut { get; set; }
            public DateTime HeureFin { get; set; }
            public int NombrePersonne { get; set; }
        }

        /// <summary>
        /// Résultat d'une modification
        /// </summary>
        public class ModificationResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }
}
