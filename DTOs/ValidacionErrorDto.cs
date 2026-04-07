namespace GeneracionApi.DTOs;

/// <summary>
/// DTO para errores de validación devueltos al frontend.
/// Cada error corresponde a un campo o nodo inválido del modelo.
/// 
/// Ejemplo de respuesta con errores:
/// {
///   "estado": "error",
///   "errores": [
///     { "campo": "clases[0].nombre", "mensaje": "El nombre es obligatorio", "etapa": "validacion" },
///     { "campo": "clases[1].atributos[0].tipo", "mensaje": "Tipo no soportado: 'foobar'", "etapa": "validacion" }
///   ]
/// }
/// </summary>
public class ValidacionErrorDto
{
    /// <summary>
    /// Código de error para identificación programática.
    /// Ejemplo: "CAMPO_REQUERIDO", "TIPO_NO_SOPORTADO", "SCHEMA_INVALIDO"
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Ruta del campo/nodo que falló.
    /// Ejemplo: "clases[0].atributos[1].tipo"
    /// </summary>
    public string Campo { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje legible del error.
    /// Ejemplo: "El tipo 'foobar' no es un tipo Java válido"
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Etapa del pipeline donde se detectó el error.
    /// Ejemplo: "validacion", "transformacion", "generacion"
    /// </summary>
    public string Etapa { get; set; } = string.Empty;
}
