using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SallseSense.Services;
using System.Threading.Tasks;

namespace SallseSense.Controllers
{
    /// <summary>
    /// Controller API pour servir les photos stockées en BLOB
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PhotoController : ControllerBase
    {
        private readonly PhotoService _photoService;
        private readonly ILogger<PhotoController> _logger;

        public PhotoController(PhotoService photoService, ILogger<PhotoController> logger)
        {
            _photoService = photoService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère une photo par son ID
        /// GET: api/photo/5
        /// </summary>
        /// <param name="id">ID de la donnée contenant la photo</param>
        /// <returns>Image JPEG ou PNG</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoBytes = await _photoService.GetPhotoByIdAsync(id);

            if (photoBytes == null || photoBytes.Length == 0)
            {
                _logger.LogWarning($"Photo {id} non trouvée");
                return NotFound();
            }

            // Détecter le type d'image selon les magic bytes
            string contentType = "image/jpeg"; // Par défaut

            if (photoBytes.Length >= 8)
            {
                // Vérifier signature PNG (89 50 4E 47 0D 0A 1A 0A)
                if (photoBytes[0] == 0x89 && photoBytes[1] == 0x50 &&
                    photoBytes[2] == 0x4E && photoBytes[3] == 0x47)
                {
                    contentType = "image/png";
                }
                // Vérifier signature JPEG (FF D8)
                else if (photoBytes[0] == 0xFF && photoBytes[1] == 0xD8)
                {
                    contentType = "image/jpeg";
                }
            }

            _logger.LogInformation($"Photo {id} servie - Type: {contentType}, Taille: {photoBytes.Length} bytes");

            // Retourner l'image avec le bon Content-Type
            return File(photoBytes, contentType);
        }

        /// <summary>
        /// Récupère la liste de toutes les photos (métadonnées seulement)
        /// GET: api/photo/list
        /// </summary>
        /// <returns>Liste des informations sur les photos</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetPhotoList()
        {
            var photos = await _photoService.GetAllPhotosAsync();
            return Ok(photos);
        }

        /// <summary>
        /// Récupère les photos d'une salle spécifique (métadonnées seulement)
        /// GET: api/photo/salle/5
        /// </summary>
        /// <param name="salleId">ID de la salle</param>
        /// <returns>Liste des informations sur les photos de la salle</returns>
        [HttpGet("salle/{salleId}")]
        public async Task<IActionResult> GetPhotosBySalle(int salleId)
        {
            var photos = await _photoService.GetPhotosBySalleAsync(salleId);
            return Ok(photos);
        }
    }
}
