using System.Net.Http.Json;
using GeneracionApi.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeneracionApi.Clients;

/// <summary>
/// Implementación de IColaboracionClient que consume la API de colaboración de Core.
/// 
/// Utiliza HttpClient con configuración inyectada para llamar al sistema Core.
/// </summary>
public class ColaboracionClient : IColaboracionClient
{
    private readonly HttpClient _httpClient;
    private readonly CoreApiSettings _settings;
    private readonly ILogger<ColaboracionClient> _logger;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// </summary>
    /// <param name="httpClient">HttpClient configurado.</param>
    /// <param name="settings">Configuración de Core API.</param>
    /// <param name="logger">Logger del cliente.</param>
    public ColaboracionClient(
        HttpClient httpClient,
        IOptions<CoreApiSettings> settings,
        ILogger<ColaboracionClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configurar la URL base del HttpClient
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = _settings.Timeout;
    }

    /// <inheritdoc/>
    public async Task<ColaboracionResponse> ObtenerColaboracionAsync(string proyectoId)
    {
        if (string.IsNullOrWhiteSpace(proyectoId))
        {
            throw new ArgumentException("El ID de proyecto no puede estar vacío.", nameof(proyectoId));
        }

        try
        {
            _logger.LogDebug("Obteniendo colaboración de Core para proyecto {ProyectoId}", proyectoId);

            var response = await _httpClient.GetAsync($"/api/colaboracion/proyectos/{proyectoId}");
            response.EnsureSuccessStatusCode();

            var colaboracion = await response.Content.ReadFromJsonAsync<ColaboracionResponse>();

            return colaboracion ?? new ColaboracionResponse { ProyectoId = proyectoId };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al obtener colaboración de Core: {Mensaje}", ex.Message);
            throw;
        }
    }
}
