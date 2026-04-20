using GeneracionApi.Clients;
using GeneracionApi.Domain;

namespace GeneracionApi.Services;

/// <summary>
/// Servicio de trazabilidad y auditoría.
///
/// Delega el registro de eventos al sistema Core a través de ITrazabilidadClient.
/// Este servicio actúa como adapter entre la aplicación y el sistema externo de trazabilidad.
///
/// REGLA: Los logs nunca se exponen al frontend.
/// Solo el administrador puede consultarlos directamente en el sistema Core.
///
/// Dependencias:
/// - ITrazabilidadClient: cliente HTTP para el sistema Core
/// - ILogger&lt;TrazabilidadService&gt;: logging de operaciones
/// </summary>
public class TrazabilidadService : ITrazabilidadService
{
    /// <summary>
    /// Tipos de evento válidos para el registro de trazabilidad.
    /// </summary>
    private static readonly HashSet<string> TiposEventoValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "inicio",
        "validacion",
        "transformacion",
        "generacion",
        "exito",
        "error"
    };

    private readonly ITrazabilidadClient _trazabilidadClient;
    private readonly ILogger<TrazabilidadService> _logger;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// </summary>
    /// <param name="trazabilidadClient">Cliente para el servicio de trazabilidad de Core.</param>
    /// <param name="logger">Logger para trazabilidad del servicio.</param>
    public TrazabilidadService(
        ITrazabilidadClient trazabilidadClient,
        ILogger<TrazabilidadService> logger)
    {
        _trazabilidadClient = trazabilidadClient ?? throw new ArgumentNullException(nameof(trazabilidadClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registra un evento en el historial de trazabilidad.
    /// Delega al sistema Core a través de ITrazabilidadClient.
    /// </summary>
    /// <param name="generacionId">ID de la generación asociada.</param>
    /// <param name="tipoEvento">Tipo de evento: "inicio", "validacion", "transformacion", "generacion", "exito", "error".</param>
    /// <param name="detalles">Detalles adicionales del evento.</param>
    /// <exception cref="ArgumentException">Cuando generacionId o tipoEvento están vacíos o tipoEvento no es válido.</exception>
    public async Task RegistrarEventoAsync(string generacionId, string tipoEvento, string detalles)
    {
        // Validar que generacionId no esté vacío
        if (string.IsNullOrWhiteSpace(generacionId))
        {
            throw new ArgumentException("El ID de generación no puede estar vacío.", nameof(generacionId));
        }

        // Validar que tipoEvento no esté vacío
        if (string.IsNullOrWhiteSpace(tipoEvento))
        {
            throw new ArgumentException("El tipo de evento no puede estar vacío.", nameof(tipoEvento));
        }

        // Validar que tipoEvento sea uno de los valores permitidos
        if (!TiposEventoValidos.Contains(tipoEvento))
        {
            var tiposValidos = string.Join(", ", TiposEventoValidos.OrderBy(t => t));
            throw new ArgumentException(
                $"El tipo de evento '{tipoEvento}' no es válido. Valores permitidos: {tiposValidos}",
                nameof(tipoEvento));
        }

        // Delegar al cliente de Core
        await _trazabilidadClient.RegistrarEventoAsync(generacionId, tipoEvento, detalles ?? string.Empty);

        // Log de la operación
        _logger.LogInformation("Evento {TipoEvento} registrado para generación {GeneracionId} via Core",
            tipoEvento, generacionId);
    }

    /// <summary>
    /// Obtiene el historial de eventos de una generación ordenados cronológicamente.
    /// Delega al sistema Core a través de ITrazabilidadClient.
    /// </summary>
    /// <param name="generacionId">ID de la generación.</param>
    /// <returns>Lista de eventos ordenada por fecha ascendente. Empty si no hay eventos.</returns>
    /// <exception cref="ArgumentException">Cuando generacionId está vacío.</exception>
    public async Task<List<object>> ObtenerHistorialAsync(string generacionId)
    {
        // Validar que generacionId no esté vacío
        if (string.IsNullOrWhiteSpace(generacionId))
        {
            throw new ArgumentException("El ID de generación no puede estar vacío.", nameof(generacionId));
        }

        // Delegar al cliente de Core
        var historial = await _trazabilidadClient.ObtenerHistorialAsync(generacionId);

        return historial;
    }
}
