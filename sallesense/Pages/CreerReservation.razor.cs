using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;
using SallseSense.Models;
using SallseSense.Services;

namespace SallseSense.Pages
{
    public partial class CreerReservation : ComponentBase
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ReservationService ReservationService { get; set; } = default!;

        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        [Inject]
        protected IDbContextFactory<Prog3A25BdSalleSenseContext> DbFactory { get; set; } = default!;

        [Parameter]
        [SupplyParameterFromQuery(Name = "salleId")]
        public int? SalleIdPreselectionne { get; set; }

        protected Reservation reservation = new Reservation { NombrePersonne = 1 };
        protected DateTime? dateReservation = DateTime.Today;
        protected TimeOnly? heureDebut = new TimeOnly(9, 0);
        protected TimeOnly? heureFin = new TimeOnly(10, 0);

        protected int currentUserId;
        protected List<SalleViewModel> sallesDisponibles = new();
        protected SalleViewModel? salleSelectionnee;
        protected List<Reservation> reservationsExistantes = new();

        protected bool depassementCapacite = false;
        protected bool salleNonDisponible = false;
        protected string errorMessage = string.Empty;
        protected string successMessage = string.Empty;

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

            // Charger les salles disponibles pour aujourd'hui
            await LoadSallesDisponiblesAsync();

            if (SalleIdPreselectionne.HasValue)
            {
                reservation.NoSalle = SalleIdPreselectionne.Value;
                await OnSalleChangedAsync();
            }
        }

        protected async Task LoadSallesDisponiblesAsync()
        {
            if (!dateReservation.HasValue || !heureDebut.HasValue || !heureFin.HasValue)
            {
                dateReservation = DateTime.Today;
                heureDebut = new TimeOnly(9, 0);
                heureFin = new TimeOnly(10, 0);
            }

            var debut = dateReservation.Value.Add(heureDebut.Value.ToTimeSpan());
            var fin = dateReservation.Value.Add(heureFin.Value.ToTimeSpan());

            // Utiliser le service pour charger les salles disponibles
            var salles = await ReservationService.GetSallesDisponiblesAsync(debut, fin);

            sallesDisponibles = salles.Select(s => new SalleViewModel
            {
                IdSallePk = s.IdSallePk,
                Numero = s.Numero,
                CapaciteMaximale = s.CapaciteMaximale,
                EstDisponible = true
            }).ToList();
        }

        protected async Task OnSalleChangedAsync()
        {
            salleSelectionnee = sallesDisponibles.FirstOrDefault(s => s.IdSallePk == reservation.NoSalle);
            VerifierCapacite();
            await ChargerReservationsExistantesAsync();
        }

        protected void VerifierCapacite()
        {
            if (salleSelectionnee != null && reservation.NombrePersonne > 0)
            {
                depassementCapacite = reservation.NombrePersonne > salleSelectionnee.CapaciteMaximale;
            }
        }

        protected void VerifierDisponibilite()
        {
            salleNonDisponible = false;
        }

        protected async Task ChargerReservationsExistantesAsync()
        {
            if (reservation.NoSalle > 0 && dateReservation.HasValue)
            {
                var dateDebut = dateReservation.Value.Date;
                var dateFin = dateReservation.Value.Date.AddDays(1);

                reservationsExistantes = await ReservationService.GetReservationsBySalleAsync(
                    reservation.NoSalle,
                    dateDebut,
                    dateFin);
            }
        }

        protected bool PeutReserver()
        {
            return reservation.NoSalle > 0 &&
                   dateReservation.HasValue &&
                   heureDebut.HasValue &&
                   heureFin.HasValue &&
                   reservation.NombrePersonne > 0 &&
                   !salleNonDisponible;
        }

        protected async Task HandleSubmit()
        {
            errorMessage = string.Empty;
            successMessage = string.Empty;

            // Validation de l'utilisateur
            if (currentUserId <= 0)
            {
                errorMessage = "Vous devez être connecté pour réserver.";
                return;
            }

            // Combiner date et heures
            if (dateReservation.HasValue && heureDebut.HasValue && heureFin.HasValue)
            {
                reservation.HeureDebut = dateReservation.Value.Add(heureDebut.Value.ToTimeSpan());
                reservation.HeureFin = dateReservation.Value.Add(heureFin.Value.ToTimeSpan());

                // Validation
                if (reservation.HeureFin <= reservation.HeureDebut)
                {
                    errorMessage = "L'heure de fin doit être après l'heure de début.";
                    return;
                }

                // Créer la réservation via le service
                var (success, reservationId, message) = await ReservationService.CreerReservationAsync(
                    currentUserId,
                    reservation.NoSalle,
                    reservation.HeureDebut,
                    reservation.HeureFin,
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

        public class SalleViewModel
        {
            public int IdSallePk { get; set; }
            public string Numero { get; set; } = string.Empty;
            public int CapaciteMaximale { get; set; }
            public bool EstDisponible { get; set; } = true;
        }
    }
}
