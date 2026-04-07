using System.Security.Cryptography;
using System.Text;

namespace GeneracionApi.Services.Pipeline.Filters;

/// <summary>
/// Filtro de registro de auditoría.
/// 
/// Es el ÚLTIMO filtro del pipeline. Registra un TraceLog con:
/// - ID de la generación
/// - Tipo de evento (exito o error)
/// - Detalles (artefactos generados, duración, etc.)
/// - Firma HMAC-SHA256 para verificar integridad
/// 
/// REGLA: TraceLog NUNCA se expone al frontend.
/// Solo el administrador puede consultar directo en MongoDB.
/// 
/// Analogía: es el "notario" que certifica lo que pasó.
/// Siempre actúa al final, después de que todo ocurrió.
/// </summary>
public class RegistroAuditoriaFilter : IFiltroGeneracion
{
    /// <summary>
    /// Clave HMAC para firmar los logs.
    /// En producción, leer de appsettings.json o variables de entorno.
    /// </summary>
    private const string CLAVE_HMAC_DEFAULT = "AGILE-GENERACION-HMAC-KEY-2026";

    public string Nombre => "RegistroAuditoria";

    public int Orden => 50;

    public Task EjecutarAsync(ContextoGeneracion contexto, Func<ContextoGeneracion, Task> siguiente)
    {
        var fechaFin = DateTime.UtcNow;
        var duracionMs = (long)(fechaFin - contexto.FechaInicio).TotalMilliseconds;

        // Determinar si hubo éxito o error
        var esExitoso = contexto.ErroresValidacion.Count == 0;
        var tipoEvento = esExitoso ? "exito" : "error";

        // Construir detalles
        var detalles = esExitoso
            ? $"Generación exitosa. Artefactos: {contexto.Artefactos.Count}. Duración: {duracionMs}ms."
            : $"Generación fallida. Errores: {contexto.ErroresValidacion.Count}. Detalles: {string.Join("; ", contexto.ErroresValidacion.Select(e => $"{e.Campo}: {e.Mensaje}"))}";

        // Crear el TraceLog
        var traceLog = new Domain.TraceLog
        {
            GeneracionId = contexto.GeneracionId,
            TipoEvento = tipoEvento,
            Detalles = detalles,
            FiltroOrigen = Nombre,
            Fecha = fechaFin
        };

        // Calcular y asignar firma HMAC
        traceLog.FirmaHmac = traceLog.CalcularFirma(CLAVE_HMAC_DEFAULT);

        // Guardar en el contexto
        contexto.LogTrazabilidad = traceLog;
        contexto.FechaFin = fechaFin;

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

        // Siempre llamamos al siguiente (si hay).
        // Si este es el último filtro, siguiente() no hace nada.
        return siguiente(contexto);
    }
}
