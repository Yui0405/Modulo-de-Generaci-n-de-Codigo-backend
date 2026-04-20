using System.Net.Http.Json;
using GeneracionApi.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeneracionApi.Clients;

/// <summary>
/// Implementación de ITrazabilidadClient que consume la API de trazabilidad de Core.
/// 
/// Utiliza HttpClient con configuración inyectada para llamar al sistema Core.
/// </summary>
public class TrazabilidadClient : ITrazabilidadClient
{
    private readonly HttpClient _httpClient;
    private readonly CoreApiSettings _settings;
    private readonly ILogger<TrazabilidadClient> _logger;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// </summary>
    /// <param name="httpClient">HttpClient configurado.</param>
    /// <param name="settings">Configuración de Core API.</param>
    /// <param name="logger">Logger del cliente.</param>
    public TrazabilidadClient(
        HttpClient httpClient,
        IOptions<CoreApiSettings> settings,
        ILogger<TrazabilidadClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configurar la URL base del HttpClient
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = _settings.Timeout;
    }

    /// <inheritdoc/>
    public async Task RegistrarEventoAsync(string generacionId, string tipoEvento, string detalles)
    {
        // Validar parámetros
        if (string.IsNullOrWhiteSpace(generacionId))
        {
            throw new ArgumentException("El ID de generación no puede estar vacío.", nameof(generacionId));
        }

        if (string.IsNullOrWhiteSpace(tipoEvento))
        {
            throw new ArgumentException("El tipo de evento no puede estar vacío.", nameof(tipoEvento));
        }

        var request = new
        {
            GeneracionId = generacionId,
            TipoEvento = tipoEvento,
            Detalles = detalles ?? string.Empty,
            Fecha = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Enviando evento {TipoEvento} a Core para generación {GeneracionId}",
                tipoEvento, generacionId);

            var response = await _httpClient.PostAsJsonAsync("/api/trazabilidad/eventos", request);

            // Throw if not successful
            response.EnsureSuccessStatusCode();

            _logger.LogDebug("Evento registrado exitosamente en Core");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al registrar evento en Core: {Mensaje}", ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<object>> ObtenerHistorialAsync(string generacionId)
    {
        if (string.IsNullOrWhiteSpace(generacionId))
        {
            throw new ArgumentException("El ID de generación no puede estar vacío.", nameof(generacionId));
        }

        try
        {
            _logger.LogDebug("Obteniendo historial de Core para generación {GeneracionId}", generacionId);

            var response = await _httpClient.GetAsync($"/api/trazabilidad/eventos/{generacionId}");
            response.EnsureSuccessStatusCode();

            var eventos = await response.Content.ReadFromJsonAsync<List<object>>();

            return eventos ?? new List<object>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al obtener historial de Core: {Mensaje}", ex.Message);
            throw;
        }
    }
}
