using GeneracionApi.Domain;

namespace GeneracionApi.Services.Pipeline;

/// <summary>
/// Objeto compartido que fluye por todo el pipeline de generación.
/// Cada filtro lee y/o escribe datos aquí.
/// 
/// Analogía: es como una "bandeja de trabajo" que pasa por cada estación
/// de una fábrica. Cada estación agrega o transforma algo en la bandeja.
/// </summary>
public class ContextoGeneracion
{
    /// <summary>
    /// Datos de la solicitud original (el JSON que envía el frontend).
    /// </summary>
    public required Diagrama Diagrama { get; init; }

    /// <summary>
    /// Configuración de generación (lenguaje, opciones, plantillas).
    /// </summary>
    public required ConfigGeneracion Configuracion { get; init; }

    /// <summary>
    /// Opciones de trazabilidad aportadas por el módulo externo de trazabilidad de A.G.I.L.E.
    /// Si está vacío, no se incrusta trazabilidad en el código generado.
    /// Si tiene datos (requisitoId, proyectoNombre, etc.), se incrustan como comentarios.
    /// </summary>
    public Dictionary<string, object> TraceabilityOptions { get; init; } = new();

    /// <summary>
    /// ID del proyecto externo (de A.G.I.L.E).
    /// Se usa para trazabilidad en el código generado.
    /// </summary>
    public string ProyectoId { get; init; } = string.Empty;

    /// <summary>
    /// ID único de esta generación (se genera al inicio).
    /// </summary>
    public string GeneracionId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Artefactos generados por el pipeline.
    /// Los filtros de generación van agregando archivos aquí.
    /// </summary>
    public List<ArtefactoGenerado> Artefactos { get; } = new();

    /// <summary>
    /// Errores de validación encontrados.
    /// Si hay errores al final de la validación, se rechaza la solicitud.
    /// </summary>
    public List<ValidationError> ErroresValidacion { get; } = new();

    /// <summary>
    /// Fecha/hora de inicio del pipeline.
    /// </summary>
    public DateTime FechaInicio { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha/hora de finalización (se establece al terminar).
    /// </summary>
    public DateTime? FechaFin { get; set; }

    /// <summary>
    /// Metadatos adicionales que los filtros pueden usar para comunicarse
    /// sin acoplarse entre sí.
    /// Ejemplo: el filtro de transformación puede dejar aquí un AST intermedio
    /// que el filtro de generación consume.
    /// </summary>
    public Dictionary<string, object> Metadatos { get; } = new();

    /// <summary>
    /// Indica si el pipeline completó exitosamente.
    /// </summary>
    public bool EsExitoso => ErroresValidacion.Count == 0;
}

/// <summary>
/// Error individual de validación. Cada filtro puede registrar
/// errores específicos por campo o nodo del modelo.
/// </summary>
public record ValidationError(
    string Campo,
    string Mensaje,
    string Etapa
);
