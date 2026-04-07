using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GeneracionApi.Domain;

/// <summary>
/// Entidad principal. Representa UNA ejecución del pipeline de generación.
/// 
/// Es el "contenedor" que agrupa:
/// - Qué modelo se usó (DiagramaId)
/// - Con qué opciones (ConfigGeneracionId)
/// - Qué se generó (ArtefactosGenerados, por relación GeneracionId)
/// - Qué pasó (TraceLogs, por relación GeneracionId)
/// 
/// El frontend identifica todo por este GeneracionId.
/// </summary>
public class Generacion
{
    /// <summary>
    /// Identificador único de esta generación.
    /// Mapea a _id en MongoDB. Se genera automáticamente.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// ID del proyecto al que pertenece esta generación.
    /// Es un ID externo — el proyecto vive en otro módulo de A.G.I.L.E.
    /// Este módulo solo lo referencia, no crea ni gestiona proyectos.
    /// </summary>
    public string ProyectoId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de modelo fuente. Determina qué pipeline se ejecuta.
    /// Valores válidos: "classDiagram", "erModel", "mvcModel"
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Estado actual de la generación.
    /// "pendiente" → pipeline en ejecución
    /// "exito"     → artefactos generados correctamente
    /// "error"     → la validación o transformación falló
    /// </summary>
    public string Estado { get; set; } = "pendiente";

    /// <summary>
    /// ID del diagrama/modelo JSON utilizado.
    /// FK hacia la colección "diagramas".
    /// </summary>
    public string DiagramaId { get; set; } = string.Empty;

    /// <summary>
    /// ID de la configuración de generación utilizada.
    /// FK hacia la colección "configuraciones".
    /// </summary>
    public string ConfigGeneracionId { get; set; } = string.Empty;

    /// <summary>
    /// ID de la generación "padre" (si esta fue creada por regenerar()).
    /// Null si es una generación original.
    /// Permite construir la cadena de regeneraciones.
    /// Ejemplo: Generacion DEF456 tiene GeneracionPadreId = "ABC123"
    /// </summary>
    public string? GeneracionPadreId { get; set; }

    /// <summary>
    /// Momento en que se inició la generación.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Momento en que terminó la generación (éxito o error).
    /// Null si aún está en proceso.
    /// </summary>
    public DateTime? FechaFin { get; set; }

    /// <summary>
    /// Cantidad de artefactos generados. Campo denormalizado
    /// para evitar contar en consultas frecuentes.
    /// </summary>
    public int CantidadArtefactos { get; set; }

    /// <summary>
    /// Mensaje de error si Estado = "error".
    /// Null si la generación fue exitosa.
    /// </summary>
    public string? MensajeError { get; set; }
}
