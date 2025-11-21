using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;
using SallseSense.Models;

namespace SallseSense.Services
{
    public class ReservationFormService
    {
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _dbFactory;
        private readonly ReservationService _reservationService;

        public ReservationFormService(
            IDbContextFactory<Prog3A25BdSalleSenseContext> dbFactory,
            ReservationService reservationService)
        {
            _dbFactory = dbFactory;
            _reservationService = reservationService;
        }

        /// <summary>
        /// Récupère les salles disponibles pour une plage horaire donnée
        /// </summary>
        public async Task<List<SalleViewModel>> GetSallesDisponiblesAsync(DateTime debut, DateTime fin)
        {
            var salles = await _reservationService.GetSallesDisponiblesAsync(debut, fin);

            return salles.Select(s => new SalleViewModel
            {
                IdSallePk = s.IdSallePk,
                Numero = s.Numero,
                CapaciteMaximale = s.CapaciteMaximale,
                EstDisponible = true
            }).ToList();
        }

        /// <summary>
        /// Récupère les réservations existantes pour une salle et une date
        /// </summary>
        public async Task<List<Reservation>> GetReservationsBySalleAsync(int noSalle, DateTime dateDebut, DateTime dateFin)
        {
            return await _reservationService.GetReservationsBySalleAsync(noSalle, dateDebut, dateFin);
        }

        /// <summary>
        /// Crée une nouvelle réservation
        /// </summary>
        public async Task<ReservationResultViewModel> CreerReservationAsync(
            int userId,
            int noSalle,
            DateTime heureDebut,
            DateTime heureFin,
            int nombrePersonne)
        {
            // Validation
            if (userId <= 0)
            {
                return new ReservationResultViewModel
                {
                    Success = false,
                    Message = "Vous devez être connecté pour réserver."
                };
            }

            if (heureFin <= heureDebut)
            {
                return new ReservationResultViewModel
                {
                    Success = false,
                    Message = "L'heure de fin doit être après l'heure de début."
                };
            }

            // Créer la réservation via le service
            var (success, reservationId, message) = await _reservationService.CreerReservationAsync(
                userId,
                noSalle,
                heureDebut,
                heureFin,
                nombrePersonne
            );

            return new ReservationResultViewModel
            {
                Success = success,
                ReservationId = reservationId,
                Message = message
            };
        }

        /// <summary>
        /// ViewModel pour une salle disponible
        /// </summary>
        public class SalleViewModel
        {
            public int IdSallePk { get; set; }
            public string Numero { get; set; } = string.Empty;
            public int CapaciteMaximale { get; set; }
            public bool EstDisponible { get; set; } = true;
        }

        /// <summary>
        /// ViewModel pour le résultat d'une création de réservation
        /// </summary>
        public class ReservationResultViewModel
        {
            public bool Success { get; set; }
            public int ReservationId { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }
}
