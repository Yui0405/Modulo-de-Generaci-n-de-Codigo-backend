using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GeneracionApi.Domain;

/// <summary>
/// Representa el modelo JSON recibido del frontend.
/// Es el "plano" que el pipeline transforma en código.
/// 
/// Ejemplo para sourceType = "classDiagram":
/// {
///   "clases": [
///     {
///       "nombre": "Usuario",
///       "atributos": [
///         { "nombre": "id", "tipo": "int" },
///         { "nombre": "nombre", "tipo": "String" }
///       ],
///       "metodos": [...],
///       "heredaDe": null,
///       "implementa": ["Serializable"]
///     }
///   ]
/// }
/// </summary>
public class Diagrama
{
    /// <summary>
    /// Identificador único del diagrama.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Tipo de diagrama: "classDiagram", "erModel", "mvcModel".
    /// Determina qué JSON Schema se usa para validación.
    /// </summary>
    public string TipoDiagrama { get; set; } = string.Empty;

    /// <summary>
    /// Nombre descriptivo del diagrama (dado por el usuario o inferido).
    /// Ejemplo: "Modelo de Usuarios", "Esquema de Ventas"
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// El contenido del modelo como JSON serializado.
    /// Se guarda como string para máxima flexibilidad,
    /// ya que la estructura varía según TipoDiagrama.
    /// </summary>
    public string ModeloJson { get; set; } = string.Empty;

    /// <summary>
    /// ID del proyecto al que pertenece este diagrama.
    /// Referencia externa a A.G.I.L.E.
    /// </summary>
    public string ProyectoId { get; set; } = string.Empty;

    /// <summary>
    /// Momento en que se registró el diagrama.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
