using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GeneracionApi.Domain;

/// <summary>
/// Registro de trazabilidad y auditoría.
/// 
/// Cada evento del pipeline genera un TraceLog:
/// - "inicio"      → cuando empieza la generación
/// - "validacion"  → resultado de la validación del modelo
/// - "transformacion" → resultado de la transformación
/// - "generacion"  → artefactos generados
/// - "exito"       → generación completada
/// - "error"       → generación falló
/// 
/// REGLA: TraceLog NUNCA se expone al frontend.
/// Solo el administrador puede consultar directo en MongoDB.
/// 
/// La FirmaHmac permite verificar que el log no fue alterado.
/// Se calcula con HMAC-SHA256 sobre los datos del evento.
/// </summary>
public class TraceLog
{
    /// <summary>
    /// Identificador único del registro de auditoría.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// ID de la generación asociada.
    /// FK hacia la colección "generaciones".
    /// </summary>
    public string GeneracionId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de evento registrado.
    /// Valores: "inicio", "validacion", "transformacion", "generacion", "exito", "error"
    /// </summary>
    public string TipoEvento { get; set; } = string.Empty;

    /// <summary>
    /// Detalles del evento en texto libre.
    /// Puede incluir mensajes de error, tiempos de ejecución,
    /// cantidad de artefactos, etc.
    /// </summary>
    public string Detalles { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del filtro que generó este log (si aplica).
    /// Ejemplo: "ValidacionMetamodeloFilter", "GeneracionArtefactosFilter"
    /// </summary>
    public string? FiltroOrigen { get; set; }

    /// <summary>
    /// Firma HMAC-SHA256 para verificar integridad del registro.
    /// Se calcula sobre: GeneracionId + TipoEvento + Detalles + Fecha
    /// Garantiza que nadie alteró el log después de creado.
    /// </summary>
    public string FirmaHmac { get; set; } = string.Empty;

    /// <summary>
    /// Momento exacto del evento.
    /// </summary>
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Calcula la firma HMAC-SHA256 del registro.
    /// </summary>
    /// <param name="claveSecreta">Clave HMAC (leer de configuración).</param>
    /// <returns>Firma en formato hexadecimal.</returns>
    public string CalcularFirma(string claveSecreta)
    {
        var datos = $"{GeneracionId}|{TipoEvento}|{Detalles}|{Fecha:O}";
        var claveBytes = Encoding.UTF8.GetBytes(claveSecreta);
        var datosBytes = Encoding.UTF8.GetBytes(datos);

        using var hmac = new HMACSHA256(claveBytes);
        var hash = hmac.ComputeHash(datosBytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Verifica que la firma almacenada sea válida.
    /// </summary>
    /// <param name="claveSecreta">Clave HMAC.</param>
    /// <returns>True si la firma es válida.</returns>
    public bool VerificarFirma(string claveSecreta)
    {
        var firmaCalculada = CalcularFirma(claveSecreta);
        return string.Equals(FirmaHmac, firmaCalculada, StringComparison.OrdinalIgnoreCase);
    }
}
