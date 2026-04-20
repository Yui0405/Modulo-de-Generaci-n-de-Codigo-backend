namespace GeneracionApi.Services;

/// <summary>
/// Contrato para el servicio de trazabilidad y auditoría.
/// 
/// Delega el registro de eventos al sistema Core a través de ITrazabilidadClient.
/// Los logs nunca se exponen al frontend — solo el administrador puede consultarlos
/// en el sistema Core.
/// </summary>
public interface ITrazabilidadService
{
    /// <summary>
    /// Registra un evento de generación (inicio, éxito o error).
    /// </summary>
    /// <param name="generacionId">ID de la generación.</param>
    /// <param name="tipoEvento">Tipo: "inicio", "exito", "error".</param>
    /// <param name="detalles">Información adicional del evento.</param>
    Task RegistrarEventoAsync(string generacionId, string tipoEvento, string detalles);

    /// <summary>
    /// Obtiene el historial de eventos de una generación.
    /// </summary>
    /// <param name="generacionId">ID de la generación.</param>
    /// <returns>Lista de eventos ordenados por fecha.</returns>
    Task<List<object>> ObtenerHistorialAsync(string generacionId);
}
