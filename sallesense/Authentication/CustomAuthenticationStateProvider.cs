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
        private readonly ProtectedLocalStorage _localStorage;
        private readonly ProtectedSessionStorage _sessionStorage;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(
            ProtectedLocalStorage localStorage,
            ProtectedSessionStorage sessionStorage)
        {
            _localStorage = localStorage;
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
                // Essayer d'abord SessionStorage (disponible immédiatement)
                var sessionResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
                var userSession = sessionResult.Success ? sessionResult.Value : null;

                // Si pas trouvé dans SessionStorage, essayer LocalStorage
                if (userSession == null)
                {
                    try
                    {
                        var localResult = await _localStorage.GetAsync<UserSession>("UserSession");
                        userSession = localResult.Success ? localResult.Value : null;

                        // Si trouvé dans LocalStorage, remettre dans SessionStorage
                        if (userSession != null)
                        {
                            await _sessionStorage.SetAsync("UserSession", userSession);
                        }
                    }
                    catch
                    {
                        // LocalStorage pas encore disponible (prerender), ignorer
                    }
                }

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
                // Connexion - créer les claims et sauvegarder dans LocalStorage ET SessionStorage
                await _localStorage.SetAsync("UserSession", userSession);
                await _sessionStorage.SetAsync("UserSession", userSession); // SessionStorage pour compatibilité
                claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.UserName),
                    new Claim(ClaimTypes.Role, userSession.Role),
                    new Claim(ClaimTypes.NameIdentifier, userSession.UserId.ToString())
                }, "CustomAuth"));
            }
            else
            {
                // Déconnexion - supprimer la session des deux storages
                await _localStorage.DeleteAsync("UserSession");
                await _sessionStorage.DeleteAsync("UserSession");
                claimsPrincipal = _anonymous;
            }

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }
    }
}
