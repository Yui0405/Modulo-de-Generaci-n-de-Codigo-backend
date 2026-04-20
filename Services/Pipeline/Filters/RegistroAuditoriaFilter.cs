namespace GeneracionApi.Services.Pipeline.Filters;

/// <summary>
/// Filtro de registro de auditoría.
/// 
/// Es el ÚLTIMO filtro del pipeline. Registra metadatos de auditoría:
/// - ID de la generación
/// - Tipo de evento (exito o error)
/// - Detalles (artefactos generados, duración, etc.)
/// 
/// La trazabilidad real se delega al sistema Core a través de ITrazabilidadClient.
/// Este filtro solo prepara los metadatos para el servicio de trazabilidad.
/// 
/// REGLA: Los logs nunca se exponen al frontend.
/// Solo el administrador puede consultarlos en el sistema Core.
/// 
/// Analogía: es el "notario" que certifica lo que pasó.
/// Siempre actúa al final, después de que todo ocurrió.
/// </summary>
public class RegistroAuditoriaFilter : IFiltroGeneracion
{
    public string Nombre => "RegistroAuditoria";

    public int Orden => 50;

    public Task EjecutarAsync(ContextoGeneracion contexto, Func<ContextoGeneracion, Task> siguiente)
    {
        var fechaFin = DateTime.UtcNow;
        var duracionMs = (long)(fechaFin - contexto.FechaInicio).TotalMilliseconds;

        // Determinar si hubo éxito o error
        var esExitoso = contexto.ErroresValidacion.Count == 0;
        var tipoEvento = esExitoso ? "exito" : "error";

        // Construir detalles para trazabilidad
        var detalles = esExitoso
            ? $"Generación exitosa. Artefactos: {contexto.Artefactos.Count}. Duración: {duracionMs}ms."
            : $"Generación fallida. Errores: {contexto.ErroresValidacion.Count}. Detalles: {string.Join("; ", contexto.ErroresValidacion.Select(e => $"{e.Campo}: {e.Mensaje}"))}";

        // Registrar metadatos de auditoría en el contexto
        contexto.Metadatos["trazabilidad"] = new
        {
            GeneracionId = contexto.GeneracionId,
            TipoEvento = tipoEvento,
            Detalles = detalles,
            FiltroOrigen = Nombre,
            Fecha = fechaFin
        };

        // Registrar métricas en metadatos para uso posterior
        contexto.Metadatos["duracionMs"] = duracionMs;
        contexto.Metadatos["filtrosEjecutados"] = new List<string>
        {
            "ValidacionMetamodelo",
            "Transformacion",
            "AplicarConfiguracion",
            "GeneracionArtefactos",
            "RegistroAuditoria"
        };

        contexto.FechaFin = fechaFin;

        // Siempre llamamos al siguiente (si hay).
        // Si este es el último filtro, siguiente() no hace nada.
        return siguiente(contexto);
    }
}
