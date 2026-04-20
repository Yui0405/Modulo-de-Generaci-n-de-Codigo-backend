namespace GeneracionApi.Clients;

/// <summary>
/// Contrato para el cliente de trazabilidad del sistema Core.
/// 
/// Este cliente consume las APIs de trazabilidad del sistema Core externo.
/// </summary>
public interface ITrazabilidadClient
{
    /// <summary>
    /// Registra un evento de trazabilidad en el sistema Core.
    /// </summary>
    /// <param name="generacionId">ID de la generación asociada.</param>
    /// <param name="tipoEvento">Tipo de evento: "inicio", "validacion", "transformacion", "generacion", "exito", "error".</param>
    /// <param name="detalles">Detalles adicionales del evento.</param>
    Task RegistrarEventoAsync(string generacionId, string tipoEvento, string detalles);

    /// <summary>
    /// Obtiene el historial de eventos de una generación desde Core.
    /// </summary>
    /// <param name="generacionId">ID de la generación.</param>
    /// <returns>Lista de eventos ordenados por fecha.</returns>
    Task<List<object>> ObtenerHistorialAsync(string generacionId);
}
