using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SallseSense.Models;
using SallseSense.Services;

namespace SallseSense.Pages
{
    public partial class ModifierReservation : ComponentBase
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ReservationService ReservationService { get; set; } = default!;

        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        [Parameter]
        public int Id { get; set; }

        private int currentUserId;
        private Reservation? reservation;
        private Reservation? reservationOriginale;
        private SalleViewModel? salleInfo;

        private int reservationSelectionneeId;
        private List<Reservation> toutesReservations = new();
        private List<Reservation> autresReservations = new();

        private DateTime? dateReservation;
        private TimeOnly? heureDebut = new TimeOnly(9, 0);
        private TimeOnly? heureFin = new TimeOnly(10, 0);

        private bool depassementCapacite = false;
        private bool salleNonDisponible = false;
        private bool afficherModalAnnulation = false;
        private string errorMessage = string.Empty;
        private string successMessage = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            // Récupérer l'utilisateur connecté
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    currentUserId = userId;
                }
            }

            // Charger la réservation depuis la base de données
            await LoadReservationAsync();
            await ChargerReservationsAsync();
        }

        private async Task LoadReservationAsync()
        {
            reservation = await ReservationService.GetReservationByIdAsync(Id);

            if (reservation == null)
            {
                errorMessage = "Réservation introuvable.";
                return;
            }

            // Vérifier que l'utilisateur est le propriétaire
            if (reservation.NoPersonne != currentUserId)
            {
                errorMessage = "Vous n'êtes pas autorisé à modifier cette réservation.";
                reservation = null;
                return;
            }

            // Copie originale pour comparaison
            reservationOriginale = new Reservation
            {
                IdReservationPk = reservation.IdReservationPk,
                NoSalle = reservation.NoSalle,
                HeureDebut = reservation.HeureDebut,
                HeureFin = reservation.HeureFin,
                NombrePersonne = reservation.NombrePersonne,
                NoPersonne = reservation.NoPersonne
            };

            // Charger les informations de la salle depuis EF Core
            await LoadSalleInfoAsync(reservation.NoSalle);

            dateReservation = reservation.HeureDebut.Date;
            heureDebut = TimeOnly.FromDateTime(reservation.HeureDebut);
            heureFin = TimeOnly.FromDateTime(reservation.HeureFin);

            reservationSelectionneeId = Id;
        }

        private async Task LoadSalleInfoAsync(int idSalle)
        {
            var salle = await ReservationService.GetSalleByIdAsync(idSalle);

            if (salle != null)
            {
                salleInfo = new SalleViewModel
                {
                    IdSallePk = salle.IdSallePk,
                    Numero = salle.Numero,
                    CapaciteMaximale = salle.CapaciteMaximale
                };
            }
        }

        private async Task ChargerReservationsAsync()
        {
            if (reservation == null) return;

            // Charger toutes les réservations futures de l'utilisateur pour cette salle
            var toutesLesReservations = await ReservationService.GetReservationsByUserAsync(currentUserId, futuresOnly: true);
            toutesReservations = toutesLesReservations
                .Where(r => r.NoSalle == reservation.NoSalle)
                .OrderBy(r => r.HeureDebut)
                .ToList();

            // Charger les autres réservations de la salle (pas de cet utilisateur)
            var dateDebut = DateTime.Now;
            var dateFin = DateTime.Now.AddMonths(1);
            var toutesReservationsSalle = await ReservationService.GetReservationsBySalleAsync(
                reservation.NoSalle,
                dateDebut,
                dateFin);

            autresReservations = toutesReservationsSalle
                .Where(r => r.NoPersonne != currentUserId && r.HeureDebut > DateTime.Now)
                .OrderBy(r => r.HeureDebut)
                .ToList();
        }

        private void OnReservationChanged(Microsoft.AspNetCore.Components.ChangeEventArgs e)
        {
            if (e.Value != null && int.TryParse(e.Value.ToString(), out int selectedId))
            {
                reservationSelectionneeId = selectedId;
                NavigationManager.NavigateTo($"/reservation/modifier/{reservationSelectionneeId}");
            }
        }

        private async Task OnDateChanged(Microsoft.AspNetCore.Components.ChangeEventArgs e)
        {
            if (e.Value != null && DateTime.TryParse(e.Value.ToString(), out DateTime newDate))
            {
                dateReservation = newDate;
                await VerifierDisponibilite();
            }
        }

        private void OnNombrePersonnesChanged(Microsoft.AspNetCore.Components.ChangeEventArgs e)
        {
            if (e.Value != null && int.TryParse(e.Value.ToString(), out int newNombre))
            {
                reservation!.NombrePersonne = newNombre;
                VerifierCapacite();
            }
        }

        private async Task OnHeureDebutChanged(Microsoft.AspNetCore.Components.ChangeEventArgs e)
        {
            if (e.Value != null && TimeOnly.TryParse(e.Value.ToString(), out TimeOnly newHeure))
            {
                heureDebut = newHeure;
                await VerifierDisponibilite();
            }
        }

        private async Task OnHeureFinChanged(Microsoft.AspNetCore.Components.ChangeEventArgs e)
        {
            if (e.Value != null && TimeOnly.TryParse(e.Value.ToString(), out TimeOnly newHeure))
            {
                heureFin = newHeure;
                await VerifierDisponibilite();
            }
        }

        private void VerifierCapacite()
        {
            if (salleInfo != null && reservation != null && reservation.NombrePersonne > 0)
            {
                depassementCapacite = reservation.NombrePersonne > salleInfo.CapaciteMaximale;
            }
        }

        private async Task VerifierDisponibilite()
        {
            salleNonDisponible = false;

            if (reservation == null || !dateReservation.HasValue || !heureDebut.HasValue || !heureFin.HasValue)
                return;

            var nouvelleHeureDebut = dateReservation.Value.Add(heureDebut.Value.ToTimeSpan());
            var nouvelleHeureFin = dateReservation.Value.Add(heureFin.Value.ToTimeSpan());

            if (nouvelleHeureFin <= nouvelleHeureDebut)
                return;

            // Vérifier si la salle est disponible pour cette nouvelle période
            var dateDebut = dateReservation.Value.Date;
            var dateFin = dateReservation.Value.Date.AddDays(1);
            var reservationsSalle = await ReservationService.GetReservationsBySalleAsync(
                reservation.NoSalle,
                dateDebut,
                dateFin);

            // Vérifier les chevauchements (sauf avec la réservation actuelle)
            salleNonDisponible = reservationsSalle
                .Where(r => r.IdReservationPk != reservation.IdReservationPk)
                .Any(r => (nouvelleHeureDebut >= r.HeureDebut && nouvelleHeureDebut < r.HeureFin)
                       || (nouvelleHeureFin > r.HeureDebut && nouvelleHeureFin <= r.HeureFin)
                       || (nouvelleHeureDebut <= r.HeureDebut && nouvelleHeureFin >= r.HeureFin));
        }

        private bool PeutModifier()
        {
            return dateReservation.HasValue &&
                   heureDebut.HasValue &&
                   heureFin.HasValue &&
                   reservation?.NombrePersonne > 0 &&
                   !salleNonDisponible;
        }

        private async Task HandleSubmit()
        {
            errorMessage = string.Empty;
            successMessage = string.Empty;

            if (dateReservation.HasValue && heureDebut.HasValue && heureFin.HasValue && reservation != null)
            {
                var nouvelleHeureDebut = dateReservation.Value.Add(heureDebut.Value.ToTimeSpan());
                var nouvelleHeureFin = dateReservation.Value.Add(heureFin.Value.ToTimeSpan());

                if (nouvelleHeureFin <= nouvelleHeureDebut)
                {
                    errorMessage = "L'heure de fin doit être après l'heure de début.";
                    return;
                }

                // Modifier la réservation via le service
                var (success, message) = await ReservationService.ModifierReservationAsync(
                    reservation.IdReservationPk,
                    currentUserId,
                    nouvelleHeureDebut,
                    nouvelleHeureFin,
                    reservation.NombrePersonne
                );

                if (success)
                {
                    successMessage = message + " Redirection...";
                    await Task.Delay(2000);
                    NavigationManager.NavigateTo("/dashboard");
                }
                else
                {
                    errorMessage = message;
                }
            }
        }

        private void ConfirmerAnnulation()
        {
            afficherModalAnnulation = true;
        }

        private async Task AnnulerReservation()
        {
            if (reservation == null) return;

            // Annuler la réservation via le service
            var (success, message) = await ReservationService.AnnulerReservationAsync(
                reservation.IdReservationPk,
                currentUserId
            );

            afficherModalAnnulation = false;

            if (success)
            {
                successMessage = message + " Redirection...";
                await Task.Delay(2000);
                NavigationManager.NavigateTo("/dashboard");
            }
            else
            {
                errorMessage = message;
            }
        }

        public class SalleViewModel
        {
            public int IdSallePk { get; set; }
            public string Numero { get; set; } = string.Empty;
            public int CapaciteMaximale { get; set; }
        }
    }
}
