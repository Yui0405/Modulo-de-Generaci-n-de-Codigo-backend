using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GeneracionApi.Domain;

/// <summary>
/// Configuración de opciones para la generación de código.
/// Define QUÉ y CÓMO se genera.
/// 
/// Ejemplo para Java:
/// {
///   "lenguajeDestino": "java",
///   "opciones": {
///     "incluirGettersSetters": true,
///     "incluirConstructores": true,
///     "convencionNombres": "camelCase",
///     "paqueteBase": "com.organizacion.modelo"
///   },
///   "outputOptions": {
///     "incluirComentarios": true,
///     "formatoSalida": "archivo"
///   }
/// }
/// </summary>
public class ConfigGeneracion
{
    /// <summary>
    /// Identificador único de la configuración.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Lenguaje de destino para la generación.
    /// Valores: "java", "sql", "mvc"
    /// </summary>
    public string LenguajeDestino { get; set; } = "java";

    /// <summary>
    /// Opciones específicas del lenguaje/framework.
    /// Varían según LenguajeDestino.
    /// Ejemplo Java: incluirGettersSetters, paqueteBase, etc.
    /// Ejemplo SQL: dialecto, incluirDropIfExists, etc.
    /// </summary>
    public Dictionary<string, object> Opciones { get; set; } = new();

    /// <summary>
    /// Opciones de salida (formato, comentarios, trazabilidad embebida).
    /// </summary>
    public Dictionary<string, object> OutputOptions { get; set; } = new();

    /// <summary>
    /// Momento en que se creó esta configuración.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
