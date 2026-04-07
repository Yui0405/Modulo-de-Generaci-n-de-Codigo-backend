namespace GeneracionApi.DTOs;

/// <summary>
/// DTO de respuesta de generación.
/// Devuelto al frontend tras una generación exitosa o fallida.
/// 
/// Este es el "contrato de salida" definido en el RFC Sección 6.
/// El servicio construye este DTO desde las entidades.
/// El controller lo serializa como JSON.
/// 
/// NOTA: Este DTO NUNCA se persiste en MongoDB.
/// Se construye en memoria desde: Generacion + Artefactos + TraceLog
/// </summary>
public class GeneracionResponseDto
{
    /// <summary>
    /// ID único de esta generación.
    /// El frontend lo usa para los demás endpoints.
    /// </summary>
    public string GeneracionId { get; set; } = string.Empty;

    /// <summary>
    /// ID del proyecto al que pertenece.
    /// </summary>
    public string ProyectoId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de modelo procesado: "classDiagram", "erModel", "mvcModel".
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Estado de la operación: "exito" o "error".
    /// </summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Lista de archivos generados.
    /// </summary>
    public List<ArchivoGeneradoDto> Archivos { get; set; } = new();

    /// <summary>
    /// Cantidad total de archivos generados.
    /// </summary>
    public int CantidadArchivos { get; set; }

    /// <summary>
    /// Marca temporal de la generación.
    /// </summary>
    public DateTime FechaGeneracion { get; set; }

    /// <summary>
    /// ID de la generación padre (si fue regeneración).
    /// Null si es generación original.
    /// </summary>
    public string? GeneracionPadreId { get; set; }

    /// <summary>
    /// Errores de validación (solo si Estado = "error").
    /// Null si la generación fue exitosa.
    /// </summary>
    public List<ValidacionErrorDto>? Errores { get; set; }

    /// <summary>
    /// Metadatos de trazabilidad.
    /// Incluye duración, filtros ejecutados, etc.
    /// </summary>
    public TrazabilidadMetadataDto? Trazabilidad { get; set; }
}

/// <summary>
/// Archivo individual dentro de un artefacto generado.
/// </summary>
public class ArchivoGeneradoDto
{
    /// <summary>
    /// Ruta lógica del archivo.
    /// Ejemplo: "src/com/uci/modelo/Usuario.java"
    /// </summary>
    public string Ruta { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del archivo.
    /// Ejemplo: "Usuario.java"
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del archivo (código fuente).
    /// </summary>
    public string Contenido { get; set; } = string.Empty;

    /// <summary>
    /// Tipo del archivo: "java-class", "sql-script", "xml-config", etc.
    /// </summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño en bytes.
    /// </summary>
    public long TamanoBytes { get; set; }
}

/// <summary>
/// Metadatos de trazabilidad incluidos en la respuesta.
/// </summary>
public class TrazabilidadMetadataDto
{
    /// <summary>
    /// Duración total de la generación en milisegundos.
    /// </summary>
    public long DuracionMs { get; set; }

    /// <summary>
    /// Nombres de los filtros ejecutados, en orden.
    /// </summary>
    public List<string> FiltrosEjecutados { get; set; } = new();

    /// <summary>
    /// ID de auditoría (TraceLog principal).
    /// </summary>
    public string? AuditoriaId { get; set; }
}
