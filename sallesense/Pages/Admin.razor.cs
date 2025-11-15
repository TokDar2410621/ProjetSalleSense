using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;
using SallseSense.Models;

namespace SallseSense.Pages
{
    public partial class Admin : ComponentBase
    {
        [Inject]
        protected IDbContextFactory<Prog3A25BdSalleSenseContext> DbFactory { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ProtectedSessionStorage SessionStorage { get; set; } = default!;

        private List<Utilisateur> utilisateurs = new();
        private HashSet<int> blacklistedUserIds = new();
        private int currentUserId;
        private bool isAdmin = false;
        private bool isLoading = true;
        private string? errorMessage;
        private string? successMessage;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Récupérer l'ID de l'utilisateur connecté depuis la session
                var userIdResult = await SessionStorage.GetAsync<int>("userId");
                if (!userIdResult.Success || userIdResult.Value == 0)
                {
                    NavigationManager.NavigateTo("/login");
                    return;
                }

                currentUserId = userIdResult.Value;

                // Charger les données
                await LoadData();
            }
            catch (Exception ex)
            {
                errorMessage = $"Erreur lors du chargement : {ex.Message}";
                isLoading = false;
            }
        }

        private async Task LoadData()
        {
            isLoading = true;
            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();

                // Vérifier si l'utilisateur est admin
                var currentUser = await db.Utilisateurs.FindAsync(currentUserId);
                if (currentUser == null)
                {
                    NavigationManager.NavigateTo("/login");
                    return;
                }

                isAdmin = currentUser.Role == "Admin";

                if (!isAdmin)
                {
                    isLoading = false;
                    return;
                }

                utilisateurs = await db.Utilisateurs
                    .OrderBy(u => u.Pseudo)
                    .Select(u => new Utilisateur
                    {
                        IdUtilisateurPk = u.IdUtilisateurPk,
                        Pseudo = u.Pseudo,
                        Courriel = u.Courriel,
                        Role = u.Role
                    })
                    .ToListAsync();

                // Charger les IDs des utilisateurs blacklistés
                blacklistedUserIds = (await db.Blacklists
                    .Select(b => b.IdUtilisateur)
                    .ToListAsync())
                    .ToHashSet();
            }
            catch (Exception ex)
            {
                errorMessage = $"Erreur lors du chargement des données : {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task AddToBlacklist(int userId)
        {
            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();

                // Utiliser la procédure stockée usp_Utilisateur_Bannir
                var pUserId = new Microsoft.Data.SqlClient.SqlParameter("@IdUtilisateur", userId);
                var pNbReservations = new Microsoft.Data.SqlClient.SqlParameter
                {
                    ParameterName = "@NbReservationsAnnulees",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Output
                };

                await db.Database.ExecuteSqlRawAsync(
                    "EXEC [dbo].[usp_Utilisateur_Bannir] @IdUtilisateur, @NbReservationsAnnulees OUTPUT",
                    pUserId, pNbReservations);

                int nbReservationsAnnulees = (int)(pNbReservations.Value ?? 0);

                successMessage = $"Utilisateur blacklisté avec succès. {nbReservationsAnnulees} réservation(s) annulée(s).";
                errorMessage = null;

                // Recharger les données
                await LoadData();
            }
            catch (Exception ex)
            {
                errorMessage = $"Erreur lors de l'ajout à la blacklist : {ex.Message}";
            }
        }

        private async Task RemoveFromBlacklist(int userId)
        {
            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();

                // Utiliser la procédure stockée usp_Utilisateur_Debloquer
                var pUserId = new Microsoft.Data.SqlClient.SqlParameter("@IdUtilisateur", userId);

                await db.Database.ExecuteSqlRawAsync(
                    "EXEC [dbo].[usp_Utilisateur_Debloquer] @IdUtilisateur",
                    pUserId);

                successMessage = "Utilisateur débloqué avec succès.";
                errorMessage = null;

                // Recharger les données
                await LoadData();
            }
            catch (Exception ex)
            {
                errorMessage = $"Erreur lors du retrait de la blacklist : {ex.Message}";
            }
        }

        private void RetourDashboard()
        {
            NavigationManager.NavigateTo("/dashboard");
        }
    }
}
