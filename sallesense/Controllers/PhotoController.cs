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
        /// <returns>Image JPEG</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoBytes = await _photoService.GetPhotoByIdAsync(id);

            if (photoBytes == null || photoBytes.Length == 0)
            {
                _logger.LogWarning($"Photo {id} non trouvée");
                return NotFound();
            }

            // Retourner l'image avec le bon Content-Type
            return File(photoBytes, "image/jpeg");
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
