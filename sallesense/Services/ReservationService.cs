using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SallseSense.Data;
using SallseSense.Models;

namespace SallseSense.Services
{
    /// <summary>
    /// Service de gestion des réservations de salles
    /// </summary>
    public class ReservationService
    {
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _factory;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(
            IDbContextFactory<Prog3A25BdSalleSenseContext> factory,
            ILogger<ReservationService> logger)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Création de réservation

        /// <summary>
        /// Crée une nouvelle réservation via la procédure stockée usp_Reservation_Creer
        /// </summary>
        /// <returns>
        /// Tuple avec:
        /// - success: true si réussi
        /// - reservationId: ID de la réservation créée (ou -1 si erreur)
        /// - message: Message d'information pour l'utilisateur
        /// </returns>
        public async Task<(bool success, int reservationId, string message)> CreerReservationAsync(
            int idUtilisateur,
            int idSalle,
            DateTime heureDebut,
            DateTime heureFin,
            int nombrePersonnes)
        {
            // Validation des entrées
            if (idUtilisateur <= 0)
                return (false, -1, "Utilisateur invalide.");

            if (idSalle <= 0)
                return (false, -1, "Salle invalide.");

            if (heureDebut >= heureFin)
                return (false, -1, "L'heure de fin doit être après l'heure de début.");

            if (nombrePersonnes <= 0)
                return (false, -1, "Le nombre de personnes doit être supérieur à zéro.");

            if (heureDebut < DateTime.Now)
                return (false, -1, "Impossible de réserver dans le passé.");

            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                // Paramètres d'entrée
                var pIdUtilisateur = new SqlParameter("@IdUtilisateur", SqlDbType.Int) { Value = idUtilisateur };
                var pIdSalle = new SqlParameter("@IdSalle", SqlDbType.Int) { Value = idSalle };
                var pHeureDebut = new SqlParameter("@HeureDebut", SqlDbType.DateTime2) { Value = heureDebut };
                var pHeureFin = new SqlParameter("@HeureFin", SqlDbType.DateTime2) { Value = heureFin };
                var pNombrePersonnes = new SqlParameter("@NombrePersonnes", SqlDbType.Int) { Value = nombrePersonnes };

                // Paramètres de sortie (OUTPUT)
                var pIdReservation = new SqlParameter("@IdReservation", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                var pCodeStatut = new SqlParameter("@CodeStatut", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                // Exécution de la procédure stockée
                await db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.usp_Reservation_Creer @IdUtilisateur, @IdSalle, @HeureDebut, @HeureFin, @NombrePersonnes, @IdReservation OUTPUT, @CodeStatut OUTPUT",
                    pIdUtilisateur, pIdSalle, pHeureDebut, pHeureFin, pNombrePersonnes, pIdReservation, pCodeStatut);

                // Récupération du résultat
                int reservationId = pIdReservation.Value != DBNull.Value ? (int)pIdReservation.Value : -1;
                int returnCode = pCodeStatut.Value != DBNull.Value ? (int)pCodeStatut.Value : -99;

                // Interprétation du code de retour
                return returnCode switch
                {
                    0 => (true, reservationId, "Réservation créée avec succès!"),
                    -1 => (false, -1, "Utilisateur introuvable."),
                    -2 => (false, -1, "Votre compte est banni. Contactez l'administrateur."),
                    -3 => (false, -1, "Salle introuvable."),
                    -4 => (false, -1, "Le nombre de personnes dépasse la capacité de la salle."),
                    -5 => (false, -1, "Cette salle est déjà réservée pour cette période."),
                    -99 => (false, -1, "Erreur système lors de la création."),
                    _ => (false, -1, "Erreur inconnue.")
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Erreur SQL lors de la création de réservation pour utilisateur {IdUtilisateur}", idUtilisateur);

                // Vérifier si c'est l'erreur du trigger de chevauchement
                if (ex.Message.Contains("Chevauchement de réservations"))
                {
                    return (false, -1, "Cette salle est déjà réservée pour cette période.");
                }

                // Vérifier si c'est l'erreur du trigger de blacklist
                if (ex.Message.Contains("est banni"))
                {
                    return (false, -1, ex.Message);
                }

                return (false, -1, $"Erreur de base de données: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la création de réservation");
                return (false, -1, $"Erreur inattendue: {ex.Message}");
            }
        }

        #endregion

        #region Annulation de réservation

        /// <summary>
        /// Annule une réservation via la procédure stockée usp_Reservation_Annuler
        /// </summary>
        public async Task<(bool success, string message)> AnnulerReservationAsync(
            int idReservation,
            int idUtilisateur)
        {
            if (idReservation <= 0)
                return (false, "Réservation invalide.");

            if (idUtilisateur <= 0)
                return (false, "Utilisateur invalide.");

            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                var pIdReservation = new SqlParameter("@IdReservation", SqlDbType.Int) { Value = idReservation };
                var pIdUtilisateur = new SqlParameter("@IdUtilisateur", SqlDbType.Int) { Value = idUtilisateur };
                var ret = new SqlParameter("@RETURN_VALUE", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };

                var sql = "EXEC @RETURN_VALUE = dbo.usp_Reservation_Annuler @IdReservation, @IdUtilisateur";

                await db.Database.ExecuteSqlRawAsync(sql, ret, pIdReservation, pIdUtilisateur);

                int returnCode = (int)(ret.Value ?? -99);

                return returnCode switch
                {
                    0 => (true, "Réservation annulée avec succès."),
                    -1 => (false, "Réservation introuvable."),
                    -2 => (false, "Vous n'êtes pas le propriétaire de cette réservation."),
                    -3 => (false, "Impossible d'annuler une réservation déjà commencée."),
                    -99 => (false, "Erreur système lors de l'annulation."),
                    _ => (false, "Erreur inconnue.")
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Erreur SQL lors de l'annulation de réservation {IdReservation}", idReservation);
                return (false, $"Erreur de base de données: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de l'annulation de réservation");
                return (false, $"Erreur inattendue: {ex.Message}");
            }
        }

        #endregion

        #region Disponibilité des salles

        /// <summary>
        /// Retourne les salles disponibles pour une période donnée via usp_Salle_Disponibilite
        /// </summary>
        public async Task<List<SalleDisponible>> GetSallesDisponiblesAsync(
            DateTime heureDebut,
            DateTime heureFin,
            int capaciteMin = 1)
        {
            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                var pHeureDebut = new SqlParameter("@HeureDebut", SqlDbType.DateTime2) { Value = heureDebut };
                var pHeureFin = new SqlParameter("@HeureFin", SqlDbType.DateTime2) { Value = heureFin };
                var pCapaciteMin = new SqlParameter("@CapaciteMin", SqlDbType.Int) { Value = capaciteMin };

                var sql = "EXEC dbo.usp_Salle_Disponibilite @HeureDebut, @HeureFin, @CapaciteMin";

                // Exécution et récupération des résultats
                var connection = db.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.Parameters.Add(pHeureDebut);
                command.Parameters.Add(pHeureFin);
                command.Parameters.Add(pCapaciteMin);

                var salles = new List<SalleDisponible>();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    salles.Add(new SalleDisponible
                    {
                        IdSallePk = reader.GetInt32(0),
                        Numero = reader.GetString(1),
                        CapaciteMaximale = reader.GetInt32(2),
                        NbCapteurs = reader.GetInt32(3)
                    });
                }

                _logger.LogInformation("Trouvé {Count} salles disponibles pour {HeureDebut} - {HeureFin}",
                    salles.Count, heureDebut, heureFin);

                return salles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche de salles disponibles");
                return new List<SalleDisponible>();
            }
        }

        #endregion

        #region Consultation de réservations

        /// <summary>
        /// Récupère toutes les réservations d'un utilisateur
        /// </summary>
        public async Task<List<Reservation>> GetReservationsByUserAsync(int idUtilisateur, bool futuresOnly = false)
        {
            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                var query = db.Reservations
                    .Where(r => r.NoPersonne == idUtilisateur);

                if (futuresOnly)
                {
                    query = query.Where(r => r.HeureFin >= DateTime.Now);
                }

                var reservations = await query
                    .OrderBy(r => r.HeureDebut)
                    .ToListAsync();

                _logger.LogInformation("Chargé {Count} réservations pour utilisateur {IdUtilisateur}",
                    reservations.Count, idUtilisateur);

                return reservations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des réservations utilisateur {IdUtilisateur}", idUtilisateur);
                return new List<Reservation>();
            }
        }

        /// <summary>
        /// Récupère toutes les réservations d'une salle
        /// </summary>
        public async Task<List<Reservation>> GetReservationsBySalleAsync(int idSalle, DateTime? dateDebut = null, DateTime? dateFin = null)
        {
            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                var query = db.Reservations
                    .Where(r => r.NoSalle == idSalle);

                if (dateDebut.HasValue)
                {
                    query = query.Where(r => r.HeureDebut >= dateDebut.Value);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(r => r.HeureFin <= dateFin.Value);
                }

                return await query
                    .OrderBy(r => r.HeureDebut)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des réservations de la salle {IdSalle}", idSalle);
                return new List<Reservation>();
            }
        }

        /// <summary>
        /// Récupère une réservation par son ID
        /// </summary>
        public async Task<Reservation?> GetReservationByIdAsync(int idReservation)
        {
            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                return await db.Reservations
                    .FirstOrDefaultAsync(r => r.IdReservationPk == idReservation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la réservation {IdReservation}", idReservation);
                return null;
            }
        }

        #endregion

        #region Modification de réservation

        /// <summary>
        /// Modifie une réservation existante
        /// </summary>
        public async Task<(bool success, string message)> ModifierReservationAsync(
            int idReservation,
            int idUtilisateur,
            DateTime nouvelleHeureDebut,
            DateTime nouvelleHeureFin,
            int nouveauNombrePersonnes)
        {
            // Validations
            if (nouvelleHeureDebut >= nouvelleHeureFin)
                return (false, "L'heure de fin doit être après l'heure de début.");

            if (nouveauNombrePersonnes <= 0)
                return (false, "Le nombre de personnes doit être supérieur à zéro.");

            if (nouvelleHeureDebut < DateTime.Now)
                return (false, "Impossible de modifier une réservation dans le passé.");

            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                // Récupérer la réservation existante
                var reservation = await db.Reservations
                    .FirstOrDefaultAsync(r => r.IdReservationPk == idReservation);

                if (reservation == null)
                    return (false, "Réservation introuvable.");

                // Vérifier que l'utilisateur est le propriétaire
                if (reservation.NoPersonne != idUtilisateur)
                    return (false, "Vous n'êtes pas le propriétaire de cette réservation.");

                // Vérifier que la réservation n'a pas commencé
                if (reservation.HeureDebut <= DateTime.Now)
                    return (false, "Impossible de modifier une réservation déjà commencée.");

                // Vérifier la capacité de la salle
                var salle = await db.Salles
                    .FirstOrDefaultAsync(s => s.IdSallePk == reservation.NoSalle);

                if (salle != null && nouveauNombrePersonnes > salle.CapaciteMaximale)
                    return (false, $"Le nombre de personnes dépasse la capacité de la salle ({salle.CapaciteMaximale}).");

                // Note: La vérification des chevauchements est gérée par le trigger trg_pasDeChevauchement
                // qui lèvera une erreur SQL si un chevauchement est détecté

                // Modifier la réservation
                reservation.HeureDebut = nouvelleHeureDebut;
                reservation.HeureFin = nouvelleHeureFin;
                reservation.NombrePersonne = nouveauNombrePersonnes;

                await db.SaveChangesAsync();

                _logger.LogInformation("Réservation {IdReservation} modifiée par utilisateur {IdUtilisateur}",
                    idReservation, idUtilisateur);

                return (true, "Réservation modifiée avec succès.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Erreur SQL lors de la modification de réservation {IdReservation}", idReservation);

                // Vérifier si c'est l'erreur du trigger de chevauchement
                if (ex.Message.Contains("Chevauchement de réservations"))
                {
                    return (false, "Cette salle est déjà réservée pour cette période.");
                }

                return (false, $"Erreur de base de données: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la modification de réservation");
                return (false, $"Erreur inattendue: {ex.Message}");
            }
        }

        #endregion

        #region Utilitaires

        /// <summary>
        /// Récupère une salle par son ID
        /// </summary>
        public async Task<Salle?> GetSalleByIdAsync(int idSalle)
        {
            try
            {
                await using var db = await _factory.CreateDbContextAsync();

                return await db.Salles
                    .FirstOrDefaultAsync(s => s.IdSallePk == idSalle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la salle {IdSalle}", idSalle);
                return null;
            }
        }

        #endregion
    }

    #region Classes de données

    /// <summary>
    /// Modèle pour les salles disponibles retournées par usp_Salle_Disponibilite
    /// </summary>
    public class SalleDisponible
    {
        public int IdSallePk { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int CapaciteMaximale { get; set; }
        public int NbCapteurs { get; set; }
    }

    #endregion
}
