using GeneracionApi.Domain;
using GeneracionApi.Repositories;

namespace GeneracionApi.Services;

/// <summary>
/// Servicio de trazabilidad y auditoría.
///
/// Gestiona el registro de eventos del pipeline de generación.
/// Implementa registro inmutable con verificación HMAC.
///
/// REGLA: TraceLog NUNCA se expone al frontend.
/// Solo el administrador puede consultarlo directamente en MongoDB.
///
/// Dependencias:
/// - IRepositorio&lt;TraceLog&gt;: persistencia de logs en MongoDB
/// - ILogger&lt;TrazabilidadService&gt;: logging de operaciones
/// </summary>
public class TrazabilidadService : ITrazabilidadService
{
    /// <summary>
    /// Clave HMAC para firmar los registros de trazabilidad.
    /// En producción, leer de appsettings.json o variables de entorno.
    /// </summary>
    private const string CLAVE_HMAC = "AGILE-GENERACION-HMAC-KEY-2026";

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

    private readonly IRepositorio<TraceLog> _repositorio;
    private readonly ILogger<TrazabilidadService> _logger;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// </summary>
    /// <param name="repositorio">Repositorio para persistencia de TraceLog.</param>
    /// <param name="logger">Logger para trazabilidad del servicio.</param>
    public TrazabilidadService(
        IRepositorio<TraceLog> repositorio,
        ILogger<TrazabilidadService> logger)
    {
        _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registra un evento en el historial de trazabilidad.
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

        // Crear la entidad TraceLog
        var traceLog = new TraceLog
        {
            GeneracionId = generacionId,
            TipoEvento = tipoEvento,
            Detalles = detalles ?? string.Empty,
            Fecha = DateTime.UtcNow
        };

        // Calcular la firma HMAC
        traceLog.FirmaHmac = traceLog.CalcularFirma(CLAVE_HMAC);

        // Persistir en MongoDB
        await _repositorio.InsertarAsync(traceLog);

        // Log de la operación
        _logger.LogInformation("Evento {TipoEvento} registrado para generación {GeneracionId}", 
            tipoEvento, generacionId);
    }

    /// <summary>
    /// Obtiene el historial de eventos de una generación ordenados cronológicamente.
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

        // Buscar todos los logs relacionados con la generación
        var logs = await _repositorio.BuscarAsync(log => log.GeneracionId == generacionId);

        // Ordenar por fecha ascendente (cronológico)
        var logsOrdenados = logs.OrderBy(log => log.Fecha).ToList();

        // Retornar como lista de objetos (casting explícito)
        return logsOrdenados.Cast<object>().ToList();
    }
}
