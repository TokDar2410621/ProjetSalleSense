using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SallseSense.Data;
using SallseSense.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SallseSense.Services
{
    /// <summary>
    /// Service pour gérer les opérations liées aux photos stockées en BLOB
    /// </summary>
    public class PhotoService
    {
        private readonly IDbContextFactory<Prog3A25BdSalleSenseContext> _contextFactory;
        private readonly ILogger<PhotoService> _logger;

        public PhotoService(
            IDbContextFactory<Prog3A25BdSalleSenseContext> contextFactory,
            ILogger<PhotoService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// Récupère toutes les photos avec leurs métadonnées
        /// </summary>
        /// <returns>Liste des photos avec informations</returns>
        public async Task<List<PhotoInfo>> GetAllPhotosAsync()
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                _logger.LogInformation("Début récupération des photos...");

                // Compter d'abord le total de lignes
                var totalDonnees = await context.Donnees.CountAsync();
                _logger.LogInformation($"Total lignes dans Donnees: {totalDonnees}");

                // Compter les photos avec BLOB non null
                var totalPhotos = await context.Donnees
                    .Where(d => d.PhotoBlob != null)
                    .CountAsync();
                _logger.LogInformation($"Photos avec photoBlob != null: {totalPhotos}");

                // Récupérer les données avec photoBlob non null
                var donnees = await context.Donnees
                    .Where(d => d.PhotoBlob != null)
                    .OrderByDescending(d => d.DateHeure)
                    .ToListAsync();

                // Filtrer et projeter en mémoire (après récupération)
                var photos = donnees
                    .Where(d => d.PhotoBlob != null && d.PhotoBlob.Length > 0)
                    .Select(d => new PhotoInfo
                    {
                        IdDonnee = d.IdDonneePk,
                        DateHeure = d.DateHeure,
                        TailleBytes = d.PhotoBlob.Length,
                        NoSalle = d.NoSalle,
                        IdCapteur = d.IdCapteur
                    })
                    .ToList();

                _logger.LogInformation($"✓ Récupération réussie de {photos.Count} photos");

                // Logger les détails des premières photos
                if (photos.Count > 0)
                {
                    _logger.LogInformation($"Première photo: ID={photos[0].IdDonnee}, Taille={photos[0].TailleBytes} bytes");
                }

                return photos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Erreur lors de la récupération des photos");
                return new List<PhotoInfo>();
            }
        }

        /// <summary>
        /// Récupère une photo spécifique par son ID
        /// </summary>
        /// <param name="idDonnee">ID de la donnée contenant la photo</param>
        /// <returns>Bytes de la photo ou null si non trouvée</returns>
        public async Task<byte[]> GetPhotoByIdAsync(int idDonnee)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var donnee = await context.Donnees
                    .Where(d => d.IdDonneePk == idDonnee)
                    .Select(d => d.PhotoBlob)
                    .FirstOrDefaultAsync();

                if (donnee == null || donnee.Length == 0)
                {
                    _logger.LogWarning($"Aucune photo trouvée pour l'ID {idDonnee}");
                    return null;
                }

                _logger.LogInformation($"Photo {idDonnee} récupérée ({donnee.Length} bytes)");
                return donnee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération de la photo {idDonnee}");
                return null;
            }
        }

        /// <summary>
        /// Récupère les photos d'une salle spécifique
        /// </summary>
        /// <param name="noSalle">Numéro de la salle</param>
        /// <returns>Liste des photos de la salle</returns>
        public async Task<List<PhotoInfo>> GetPhotosBySalleAsync(int noSalle)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // Récupérer d'abord les données
                var donnees = await context.Donnees
                    .Where(d => d.NoSalle == noSalle && d.PhotoBlob != null)
                    .OrderByDescending(d => d.DateHeure)
                    .ToListAsync();

                // Filtrer et projeter en mémoire
                var photos = donnees
                    .Where(d => d.PhotoBlob != null && d.PhotoBlob.Length > 0)
                    .Select(d => new PhotoInfo
                    {
                        IdDonnee = d.IdDonneePk,
                        DateHeure = d.DateHeure,
                        TailleBytes = d.PhotoBlob.Length,
                        NoSalle = d.NoSalle,
                        IdCapteur = d.IdCapteur
                    })
                    .ToList();

                _logger.LogInformation($"Récupération de {photos.Count} photos pour la salle {noSalle}");
                return photos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des photos de la salle {noSalle}");
                return new List<PhotoInfo>();
            }
        }

        /// <summary>
        /// Convertit une photo en chaîne Base64 pour affichage HTML
        /// </summary>
        /// <param name="photoBytes">Bytes de la photo</param>
        /// <returns>Chaîne Base64 avec préfixe data:image</returns>
        public string ConvertToBase64Image(byte[] photoBytes)
        {
            if (photoBytes == null || photoBytes.Length == 0)
                return null;

            var base64 = Convert.ToBase64String(photoBytes);
            // pourquoi ça n'arrive pas à convertir les photos ? peutre du a la maniere dont mon code python change  les photos en bineaire
            return $"data:image/jpeg;base64,{base64}";
        }

        /// <summary>
        /// Insère une nouvelle photo dans la base de données
        /// </summary>
        /// <param name="photoBytes">Bytes de la photo</param>
        /// <param name="idCapteur">ID du capteur</param>
        /// <param name="noSalle">Numéro de la salle</param>
        /// <returns>ID de la donnée crée, ou 0 si échec</returns>
        public async Task<int> InsertPhotoAsync(byte[] photoBytes, int idCapteur, int noSalle)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                var donnee = new Donnee
                {
                    DateHeure = DateTime.Now,
                    IdCapteur = idCapteur,
                    Mesure = null,
                    PhotoBlob = photoBytes,
                    NoSalle = noSalle
                };

                context.Donnees.Add(donnee);
                await context.SaveChangesAsync();

                _logger.LogInformation($"Photo insérée avec succès - ID: {donnee.IdDonneePk}, Taille: {photoBytes.Length} bytes");
                return donnee.IdDonneePk;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'insertion de la photo");
                return 0;
            }
        }
    }

    /// <summary>
    /// Classe pour représenter les informations d'une photo sans les données binaires
    /// </summary>
    public class PhotoInfo
    {
        public int IdDonnee { get; set; }
        public DateTime DateHeure { get; set; }
        public int TailleBytes { get; set; }
        public int NoSalle { get; set; }
        public int IdCapteur { get; set; }

        /// <summary>
        /// Taille de la photo en KB
        /// </summary>
        public double TailleKB => TailleBytes / 1024.0;

        /// <summary>
        /// Taille formatée pour affichage
        /// </summary>
        public string TailleFormatee => $"{TailleKB:F1} KB";
    }
}
