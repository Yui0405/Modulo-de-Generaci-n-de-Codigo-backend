namespace GeneracionApi.DTOs;

/// <summary>
/// DTO de solicitud de generación.
/// Recibido desde el frontend vía POST /api/generacion.
/// 
/// Este es el "contrato de entrada" definido en el RFC Sección 5.
/// El controller recibe este DTO y lo pasa al servicio.
/// El servicio lo convierte en entidades de dominio.
/// 
/// NOTA: Este DTO NUNCA se persiste en MongoDB.
/// Se desarma en: Diagrama + ConfigGeneracion + Generacion
/// </summary>
public class GeneracionRequestDto
{
    /// <summary>
    /// ID del proyecto (externo, de A.G.I.L.E).
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de modelo fuente.
    /// Valores: "classDiagram", "erModel", "mvcModel"
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// El modelo JSON como string.
    /// El pipeline lo validará contra el JSON Schema correspondiente.
    /// </summary>
    public string ModelJson { get; set; } = string.Empty;

    /// <summary>
    /// Opciones de generación (lenguaje, plantillas, configuración).
    /// Ejemplo: { "incluirGettersSetters": true, "paqueteBase": "com.organizacion" }
    /// </summary>
    public Dictionary<string, object> GenerationOptions { get; set; } = new();

    /// <summary>
    /// Opciones de trazabilidad.
    /// Ejemplo: { "incluirMetadatos": true, "formatoComentarios": "javadoc" }
    /// </summary>
    public Dictionary<string, object> TraceabilityOptions { get; set; } = new();

    /// <summary>
    /// Opciones de salida (formato, estructura de carpetas).
    /// Ejemplo: { "incluirComentarios": true, "estructuraPaquetes": true }
    /// </summary>
    public Dictionary<string, object> OutputOptions { get; set; } = new();
}
