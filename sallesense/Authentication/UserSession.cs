namespace SallseSense.Authentication
{
    /// <summary>
    /// Modèle de session utilisateur contenant les informations de l'utilisateur connecté
    /// </summary>
    public class UserSession
    {
        /// <summary>
        /// Nom d'utilisateur (pseudo ou courriel)
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Rôle de l'utilisateur (Utilisateur ou Admin)
        /// </summary>
        public string Role { get; set; } = "Utilisateur";

        /// <summary>
        /// ID de l'utilisateur dans la base de données
        /// </summary>
        public int UserId { get; set; }
    }
}
