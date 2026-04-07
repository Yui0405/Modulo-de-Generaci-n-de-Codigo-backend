using GeneracionApi.DTOs;

namespace GeneracionApi.Services;

/// <summary>
/// Contrato para consultar el estado de integración de un proyecto
/// con el módulo de generación.
/// 
/// Este servicio responde a: GET /api/generacion/integracion/{proyectoId}
/// El frontend lo usa para mostrar el estado de conectividad y configuración
/// del módulo de generación para un proyecto específico.
/// </summary>
public interface IIntegracionService
{
    /// <summary>
    /// Consulta el estado de integración de un proyecto.
    /// </summary>
    /// <param name="proyectoId">ID del proyecto a consultar.</param>
    /// <returns>Información de integración (estado, última generación, etc.).</returns>
    Task<object?> ConsultarAsync(string proyectoId);
}
