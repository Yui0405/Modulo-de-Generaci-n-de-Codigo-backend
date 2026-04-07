using GeneracionApi.DTOs;
using GeneracionApi.Services;
using GeneracionApi.Services.Pipeline;
using Microsoft.AspNetCore.Mvc;

namespace GeneracionApi.Controllers;

/// <summary>
/// Controlador API para operaciones de generación de código.
///
/// Expone los endpoints definidos en el RFC Sección 4:
/// - POST /api/generacion → nueva generación
/// - GET /api/generacion/{id} → consultar generación
/// - GET /api/generacion/{id}/archivo → obtener archivo específico
/// - GET /api/generacion/{id}/descargar → descargar ZIP
/// - POST /api/generacion/{id}/regenerar → regenerar código
/// - GET /api/generacion/integracion/{proyectoId} → estado de integración
/// 
/// Este controller orquestra las llamadas al servicio de generación.
/// No conoce del pipeline ni de la persistencia.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GeneracionController : ControllerBase
{
    private readonly IGeneracionService _generacionService;
    private readonly IIntegracionService _integracionService;
    private readonly ILogger<GeneracionController> _logger;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// </summary>
    /// <param name="generacionService">Servicio principal de generación.</param>
    /// <param name="integracionService">Servicio de integración.</param>
    /// <param name="logger">Logger del controller.</param>
    public GeneracionController(
        IGeneracionService generacionService,
        IIntegracionService integracionService,
        ILogger<GeneracionController> logger)
    {
        _generacionService = generacionService;
        _integracionService = integracionService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/generacion/health
    /// 
    /// Health check para verificar que la API está funcionando.
    /// </summary>
    /// <returns>200 OK si la API está disponible.</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "OK", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// POST /api/generacion
    /// 
    /// Inicia una nueva generación de código a partir de un modelo JSON.
    /// El modelo se valida, transforma y se generan los artefactos.
    /// </summary>
    /// <param name="request">DTO con el modelo, opciones y configuración.</param>
    /// <returns>201 Created con los artefactos generados o 400 con errores.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(GeneracionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerarAsync([FromBody] GeneracionRequestDto request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { message = "El cuerpo de la solicitud no puede ser nulo." });
            }

            _logger.LogInformation("Iniciando generación para proyecto {ProyectoId}", request.ProjectId);

            var resultado = await _generacionService.GenerarAsync(request);

            _logger.LogInformation("Generación {GeneracionId} completada con estado {Estado}",
                resultado.GeneracionId, resultado.Estado);

            return CreatedAtAction(nameof(ObtenerPorIdAsync), new { generacionId = resultado.GeneracionId }, resultado);
        }
        catch (GeneracionException ex)
        {
            _logger.LogWarning(ex, "Errores de validación en generación");
            return BuildValidationProblemResponse(ex.Errores);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Argumento nulo en generación");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido en generación");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en generación");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/generacion/{generacionId}
    /// 
    /// Recupera una generación previamente ejecutada por su ID.
    /// </summary>
    /// <param name="generacionId">ID único de la generación.</param>
    /// <returns>200 OK con el DTO de respuesta o 404 Not Found.</returns>
    [HttpGet("{generacionId}")]
    [ProducesResponseType(typeof(GeneracionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerPorIdAsync(string generacionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(generacionId))
            {
                return BadRequest(new { message = "El ID de generación no puede estar vacío." });
            }

            var resultado = await _generacionService.ObtenerPorIdAsync(generacionId);

            if (resultado == null)
            {
                _logger.LogWarning("Generación {GeneracionId} no encontrada", generacionId);
                return NotFound(new { message = $"No se encontró la generación con ID: {generacionId}" });
            }

            return Ok(resultado);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Argumento nulo al obtener generación");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido al obtener generación");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener generación {GeneracionId}", generacionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/generacion/{generacionId}/archivo
    /// 
    /// Obtiene el contenido de un archivo específico dentro de una generación.
    /// </summary>
    /// <param name="generacionId">ID único de la generación.</param>
    /// <param name="ruta">Ruta lógica del archivo dentro del artefacto.</param>
    /// <returns>200 OK con el contenido del archivo o 404 Not Found.</returns>
    [HttpGet("{generacionId}/archivo")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerArchivoAsync(string generacionId, [FromQuery] string ruta)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(generacionId))
            {
                return BadRequest(new { message = "El ID de generación no puede estar vacío." });
            }

            if (string.IsNullOrWhiteSpace(ruta))
            {
                return BadRequest(new { message = "La ruta del archivo no puede estar vacía." });
            }

            var contenido = await _generacionService.ObtenerArchivoAsync(generacionId, ruta);

            if (contenido == null)
            {
                _logger.LogWarning("Archivo {Ruta} no encontrado en generación {GeneracionId}", ruta, generacionId);
                return NotFound(new { message = $"No se encontró el archivo '{ruta}' en la generación '{generacionId}'" });
            }

            return Ok(contenido);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Argumento nulo al obtener archivo");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido al obtener archivo");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener archivo {Ruta} de generación {GeneracionId}", ruta, generacionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/generacion/{generacionId}/descargar
    /// 
    /// Descarga todos los archivos de una generación en formato ZIP.
    /// </summary>
    /// <param name="generacionId">ID único de la generación.</param>
    /// <returns>200 OK con el archivo ZIP o 404 Not Found.</returns>
    [HttpGet("{generacionId}/descargar")]
    [Produces("application/zip")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DescargarZipAsync(string generacionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(generacionId))
            {
                return BadRequest(new { message = "El ID de generación no puede estar vacío." });
            }

            var zipBytes = await _generacionService.DescargarZipAsync(generacionId);

            if (zipBytes == null)
            {
                _logger.LogWarning("Generación {GeneracionId} no encontrada para descarga", generacionId);
                return NotFound(new { message = $"No se encontró la generación con ID: {generacionId}" });
            }

            _logger.LogInformation("Generando descarga ZIP para generación {GeneracionId}", generacionId);

            return File(zipBytes, "application/zip", $"generacion-{generacionId}.zip");
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Argumento nulo al descargar ZIP");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido al descargar ZIP");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al descargar ZIP para generación {GeneracionId}", generacionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/generacion/{generacionId}/regenerar
    /// 
    /// Regenera el código usando la misma configuración de una generación anterior.
    /// Crea una nueva generación (no modifica la existente).
    /// </summary>
    /// <param name="generacionId">ID de la generación original.</param>
    /// <returns>201 Created con la nueva generación o 404 Not Found.</returns>
    [HttpPost("{generacionId}/regenerar")]
    [ProducesResponseType(typeof(GeneracionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegenerarAsync(string generacionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(generacionId))
            {
                return BadRequest(new { message = "El ID de generación no puede estar vacío." });
            }

            _logger.LogInformation("Iniciando regeneración de generación {GeneracionId}", generacionId);

            var resultado = await _generacionService.RegenerarAsync(generacionId);

            if (resultado == null)
            {
                _logger.LogWarning("Generación {GeneracionId} no encontrada para regeneración", generacionId);
                return NotFound(new { message = $"No se encontró la generación con ID: {generacionId}" });
            }

            _logger.LogInformation("Regeneración {NuevaGeneracionId} completada desde generación padre {GeneracionId}",
                resultado.GeneracionId, generacionId);

            return CreatedAtAction(nameof(ObtenerPorIdAsync), new { generacionId = resultado.GeneracionId }, resultado);
        }
        catch (GeneracionException ex)
        {
            _logger.LogWarning(ex, "Errores de validación en regeneración");
            return BuildValidationProblemResponse(ex.Errores);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Argumento nulo en regeneración");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido en regeneración");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en regeneración de generación {GeneracionId}", generacionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/generacion/integracion/{proyectoId}
    /// 
    /// Consulta el estado de integración de un proyecto con el módulo de generación.
    /// </summary>
    /// <param name="proyectoId">ID del proyecto en A.G.I.L.E.</param>
    /// <returns>200 OK con la información de integración o 400 Bad Request.</returns>
    [HttpGet("integracion/{proyectoId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConsultarIntegracionAsync(string proyectoId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(proyectoId))
            {
                return BadRequest(new { message = "El ID del proyecto no puede estar vacío." });
            }

            _logger.LogInformation("Consultando integración para proyecto {ProyectoId}", proyectoId);

            var resultado = await _integracionService.ConsultarAsync(proyectoId);

            return Ok(resultado);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "Argumento nulo al consultar integración");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Argumento inválido al consultar integración");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al consultar integración para proyecto {ProyectoId}", proyectoId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Construye una respuesta de tipo ProblemDetails con errores de validación.
    /// </summary>
    /// <param name="errores">Lista de errores de validación.</param>
    /// <returns>400 Bad Request con ValidationProblem.</returns>
    private IActionResult BuildValidationProblemResponse(List<ValidationError> errores)
    {
        foreach (var error in errores)
        {
            ModelState.AddModelError(error.Campo, error.Mensaje);
        }

        return ValidationProblem(ModelState);
    }
}