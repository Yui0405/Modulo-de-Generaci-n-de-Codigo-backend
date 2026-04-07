using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GeneracionApi.Domain;

/// <summary>
/// Representa UN archivo generado por el pipeline.
/// 
/// Cada artefacto es un archivo individual del resultado:
/// - Una clase Java (ej: "src/com/uci/modelo/Usuario.java")
/// - Un script SQL (ej: "schema/create_tables.sql")
/// - Un archivo de configuración (ej: "pom.xml")
/// 
/// Todos los artefactos de una generación se agrupan por GeneracionId.
/// El contenido se guarda como string en MongoDB.
/// El ZIP se genera on-demand leyendo estos artefactos.
/// </summary>
public class ArtefactoGenerado
{
    /// <summary>
    /// Identificador único del artefacto.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// ID de la generación a la que pertenece.
    /// FK hacia la colección "generaciones".
    /// </summary>
    public string GeneracionId { get; set; } = string.Empty;

    /// <summary>
    /// Ruta lógica del archivo dentro de la estructura generada.
    /// Ejemplo: "src/com/uci/modelo/Usuario.java"
    /// Ejemplo: "schema/create_tables.sql"
    /// Se usa para crear la estructura de carpetas en el ZIP.
    /// </summary>
    public string Ruta { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del archivo (sin la ruta).
    /// Ejemplo: "Usuario.java", "create_tables.sql"
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Contenido completo del archivo de texto generado.
    /// Para clases Java, scripts SQL y configuraciones, el tamaño
    /// es típicamente de 1-10KB. No requiere GridFS.
    /// </summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>
    /// Tipo/categoría del archivo.
    /// Ejemplo: "java-class", "sql-script", "xml-config", "json-config"
    /// </summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño del contenido en bytes (denormalizado para consultas rápidas).
    /// </summary>
    public long TamanoBytes { get; set; }

    /// <summary>
    /// Momento en que se generó este artefacto.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
