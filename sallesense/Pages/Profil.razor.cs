using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SallseSense.Data;
using SallseSense.Models;

namespace SallseSense.Pages
{
    public partial class Profil : ComponentBase
    {
        [Inject]
        protected AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        [Inject]
        protected IDbContextFactory<Prog3A25BdSalleSenseContext> DbFactory { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        private Utilisateur? utilisateur;
        private UtilisateurEditModel utilisateurEdit = new();
        private MotDePasseModel motDePasseModel = new();

        private bool isLoading = true;
        private bool modeEdition = false;
        private bool modeChangementMotDePasse = false;
        private bool isSubmitting = false;
        private bool isSubmittingPassword = false;

        private string messageSucces = string.Empty;
        private string messageErreur = string.Empty;

        private int userId;

        protected override async Task OnInitializedAsync()
        {
            await ChargerUtilisateur();
        }

        private async Task ChargerUtilisateur()
        {
            isLoading = true;
            try
            {
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
                    {
                        await using var db = await DbFactory.CreateDbContextAsync();
                        utilisateur = await db.Utilisateurs
                            .FirstOrDefaultAsync(u => u.IdUtilisateurPk == userId);
                    }
                }
            }
            catch (Exception ex)
            {
                messageErreur = $"Erreur lors du chargement: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }

        private void ActiverModeEdition()
        {
            if (utilisateur != null)
            {
                utilisateurEdit = new UtilisateurEditModel
                {
                    Pseudo = utilisateur.Pseudo,
                    Courriel = utilisateur.Courriel
                };
                modeEdition = true;
                messageSucces = string.Empty;
                messageErreur = string.Empty;
            }
        }

        private void AnnulerEdition()
        {
            modeEdition = false;
            utilisateurEdit = new();
        }

        private async Task SauvegarderModifications()
        {
            isSubmitting = true;
            messageSucces = string.Empty;
            messageErreur = string.Empty;

            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();

                // Paramètres pour la procédure stockée
                var pIdUtilisateur = new Microsoft.Data.SqlClient.SqlParameter("@IdUtilisateur", System.Data.SqlDbType.Int)
                {
                    Value = userId
                };

                var pNouveauPseudo = new Microsoft.Data.SqlClient.SqlParameter("@NouveauPseudo", System.Data.SqlDbType.NVarChar, 40)
                {
                    Value = utilisateurEdit.Pseudo
                };

                var pNouveauCourriel = new Microsoft.Data.SqlClient.SqlParameter("@NouveauCourriel", System.Data.SqlDbType.NVarChar, 40)
                {
                    Value = utilisateurEdit.Courriel
                };

                var pCodeStatut = new Microsoft.Data.SqlClient.SqlParameter("@CodeStatut", System.Data.SqlDbType.Int)
                {
                    Direction = System.Data.ParameterDirection.Output
                };

                // Exécuter la procédure stockée
                await db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.usp_Utilisateur_Modifier @IdUtilisateur, @NouveauPseudo, @NouveauCourriel, @CodeStatut OUTPUT",
                    pIdUtilisateur, pNouveauPseudo, pNouveauCourriel, pCodeStatut);

                // Récupérer le code de retour
                int codeStatut = pCodeStatut.Value != DBNull.Value ? (int)pCodeStatut.Value : -99;

                // Interpréter le résultat
                switch (codeStatut)
                {
                    case 0:
                        // Recharger les données
                        await ChargerUtilisateur();
                        modeEdition = false;
                        messageSucces = "Vos informations ont été mises à jour avec succès!";
                        break;
                    case -1:
                        messageErreur = "Utilisateur introuvable.";
                        break;
                    case -2:
                        messageErreur = "Ce pseudo est déjà utilisé par un autre utilisateur.";
                        break;
                    case -3:
                        messageErreur = "Cet email est déjà utilisé par un autre utilisateur.";
                        break;
                    case -99:
                        messageErreur = "Erreur système lors de la modification.";
                        break;
                    default:
                        messageErreur = "Erreur inconnue.";
                        break;
                }
            }
            catch (Exception ex)
            {
                messageErreur = $"Erreur lors de la sauvegarde: {ex.Message}";
            }
            finally
            {
                isSubmitting = false;
            }
        }

        private void AnnulerChangementMotDePasse()
        {
            modeChangementMotDePasse = false;
            motDePasseModel = new();
        }

        private async Task ChangerMotDePasse()
        {
            isSubmittingPassword = true;
            messageSucces = string.Empty;
            messageErreur = string.Empty;

            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(motDePasseModel.AncienMotDePasse))
                {
                    messageErreur = "Veuillez entrer votre ancien mot de passe.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(motDePasseModel.NouveauMotDePasse))
                {
                    messageErreur = "Veuillez entrer un nouveau mot de passe.";
                    return;
                }

                if (motDePasseModel.NouveauMotDePasse != motDePasseModel.ConfirmationMotDePasse)
                {
                    messageErreur = "Les mots de passe ne correspondent pas.";
                    return;
                }

                if (motDePasseModel.NouveauMotDePasse.Length < 6)
                {
                    messageErreur = "Le mot de passe doit contenir au moins 6 caractères.";
                    return;
                }

                await using var db = await DbFactory.CreateDbContextAsync();

                // Générer nouveau salt et calculer le hash (comme Reset_Password_Admin_Correct.sql)
                var nouveauSaltBytes = new byte[16];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(nouveauSaltBytes);
                }

                var nouveauHashBytes = await db.Database.SqlQueryRaw<byte[]>(
                    @"SELECT HASHBYTES('SHA2_256', {0} + CONVERT(VARBINARY(4000), {1})) AS Value",
                    nouveauSaltBytes,
                    motDePasseModel.NouveauMotDePasse
                ).FirstOrDefaultAsync();

                if (nouveauHashBytes == null || nouveauHashBytes.Length == 0)
                {
                    messageErreur = "Erreur lors du calcul du hash du mot de passe.";
                    return;
                }

                // Paramètres pour la procédure stockée
                var pIdUtilisateur = new Microsoft.Data.SqlClient.SqlParameter("@IdUtilisateur", System.Data.SqlDbType.Int)
                {
                    Value = userId
                };

                var pAncienMotDePasse = new Microsoft.Data.SqlClient.SqlParameter("@AncienMotDePasse", System.Data.SqlDbType.NVarChar, -1)
                {
                    Value = motDePasseModel.AncienMotDePasse
                };

                var pNouveauSalt = new Microsoft.Data.SqlClient.SqlParameter("@NouveauSalt", System.Data.SqlDbType.VarBinary, 16)
                {
                    Value = nouveauSaltBytes
                };

                var pNouveauHash = new Microsoft.Data.SqlClient.SqlParameter("@NouveauHash", System.Data.SqlDbType.VarBinary, 32)
                {
                    Value = nouveauHashBytes
                };

                var pCodeStatut = new Microsoft.Data.SqlClient.SqlParameter("@CodeStatut", System.Data.SqlDbType.Int)
                {
                    Direction = System.Data.ParameterDirection.Output
                };

                // Exécuter la procédure stockée
                await db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.usp_Utilisateur_ChangerMotDePasse @IdUtilisateur, @AncienMotDePasse, @NouveauSalt, @NouveauHash, @CodeStatut OUTPUT",
                    pIdUtilisateur, pAncienMotDePasse, pNouveauSalt, pNouveauHash, pCodeStatut);

                // Récupérer le code de retour
                int codeStatut = pCodeStatut.Value != DBNull.Value ? (int)pCodeStatut.Value : -99;

                // Interpréter le résultat
                switch (codeStatut)
                {
                    case 0:
                        modeChangementMotDePasse = false;
                        motDePasseModel = new();
                        messageSucces = "Votre mot de passe a été changé avec succès!";
                        break;
                    case -1:
                        messageErreur = "Utilisateur introuvable.";
                        break;
                    case -2:
                        messageErreur = "L'ancien mot de passe est incorrect.";
                        break;
                    case -99:
                        messageErreur = "Erreur système lors du changement de mot de passe.";
                        break;
                    default:
                        messageErreur = "Erreur inconnue.";
                        break;
                }
            }
            catch (Exception ex)
            {
                messageErreur = $"Erreur lors du changement de mot de passe: {ex.Message}";
            }
            finally
            {
                isSubmittingPassword = false;
            }
        }

        // Modèles pour les formulaires
        public class UtilisateurEditModel
        {
            public string Pseudo { get; set; } = string.Empty;
            public string Courriel { get; set; } = string.Empty;
        }

        public class MotDePasseModel
        {
            public string AncienMotDePasse { get; set; } = string.Empty;
            public string NouveauMotDePasse { get; set; } = string.Empty;
            public string ConfirmationMotDePasse { get; set; } = string.Empty;
        }
    }
}
