using GeneracionApi.Domain;
using GeneracionApi.Repositories;
using Microsoft.Extensions.Logging;

namespace GeneracionApi.Services;

/// <summary>
/// Servicio para consultar el estado de integración de un proyecto
/// con el módulo de generación.
/// 
/// Este servicio responde a: GET /api/generacion/integracion/{proyectoId}
/// El frontend lo usa para mostrar el estado de conectividad y configuración
/// del módulo de generación para un proyecto específico.
/// </summary>
public class IntegracionService : IIntegracionService
{
    private readonly IRepositorio<Generacion> _repositorio;
    private readonly ILogger<IntegracionService> _logger;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// </summary>
    /// <param name="repositorio">Repositorio para persistencia de Generacion.</param>
    /// <param name="logger">Logger del servicio.</param>
    public IntegracionService(
        IRepositorio<Generacion> repositorio,
        ILogger<IntegracionService> logger)
    {
        _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<object?> ConsultarAsync(string proyectoId)
    {
        // RN-025: Return null if proyectoId is empty or null
        if (string.IsNullOrWhiteSpace(proyectoId))
        {
            return null;
        }

        _logger.LogInformation("Consultando integración para proyecto {ProyectoId}", proyectoId);

        // Count generations by ProyectoId
        var totalGeneraciones = await _repositorio.ContarAsync(g => g.ProyectoId == proyectoId);

        // Get last generation ordered by FechaCreacion DESC using efficient MongoDB query
        var ultimaGeneracion = await _repositorio.ObtenerUltimaAsync(
            g => g.ProyectoId == proyectoId,
            g => g.FechaCreacion);

        // InferEstadoIntegracion
        string estadoIntegracion;
        if (totalGeneraciones == 0)
        {
            // RN-022: sin_integracion if count == 0
            estadoIntegracion = "sin_integracion";
        }
        else if (ultimaGeneracion?.Estado == "error")
        {
            // RN-023: error if last generation Estado == "error"
            estadoIntegracion = "error";
        }
        else
        {
            // RN-024: activo otherwise (at least one successful)
            estadoIntegracion = "activo";
        }

        // Build response
        return new
        {
            ProyectoId = proyectoId,
            TotalGeneraciones = totalGeneraciones,
            EstadoIntegracion = estadoIntegracion,
            UltimaGeneracion = ultimaGeneracion != null ? new
            {
                Id = ultimaGeneracion.Id,
                SourceType = ultimaGeneracion.SourceType,
                Estado = ultimaGeneracion.Estado,
                DiagramaId = ultimaGeneracion.DiagramaId,
                ConfigGeneracionId = ultimaGeneracion.ConfigGeneracionId,
                CantidadArtefactos = ultimaGeneracion.CantidadArtefactos,
                MensajeError = ultimaGeneracion.MensajeError
            } : null,
            FechaUltimaGeneracion = ultimaGeneracion?.FechaCreacion
        };
    }
}
