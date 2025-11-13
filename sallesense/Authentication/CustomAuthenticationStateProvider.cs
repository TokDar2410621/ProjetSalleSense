using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SallseSense.Authentication
{
    /// <summary>
    /// Fournisseur d'authentification personnalisé pour gérer les sessions utilisateur
    /// </summary>
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        /// <summary>
        /// Récupère l'état d'authentification actuel de l'utilisateur depuis la session
        /// </summary>
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var claimsPrincipal = _anonymous;
            try
            {
                var userSessionStorageResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
                var userSession = userSessionStorageResult.Success ? userSessionStorageResult.Value : null;

                if (userSession != null)
                {
                    claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, userSession.UserName),
                        new Claim(ClaimTypes.Role, userSession.Role),
                        new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString())
                    }, "CustomAuth"));
                }
            }
            catch
            {
                claimsPrincipal = _anonymous;
            }

            return await Task.FromResult(new AuthenticationState(claimsPrincipal));
        }

        /// <summary>
        /// Met à jour l'état d'authentification de l'utilisateur
        /// </summary>
        /// <param name="userSession">Session utilisateur (null pour déconnexion)</param>
        public async Task UpdateAuthenticationState(UserSession? userSession)
        {
            ClaimsPrincipal claimsPrincipal;

            if (userSession != null)
            {
                // Connexion - créer les claims
                await _sessionStorage.SetAsync("UserSession", userSession);
                claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.UserName),
                    new Claim(ClaimTypes.Role, userSession.Role),
                    new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString())
                }, "CustomAuth"));
            }
            else
            {
                // Déconnexion - supprimer la session
                await _sessionStorage.DeleteAsync("UserSession");
                claimsPrincipal = _anonymous;
            }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }
    }
}
