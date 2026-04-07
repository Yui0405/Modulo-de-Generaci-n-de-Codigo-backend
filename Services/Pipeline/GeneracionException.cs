namespace GeneracionApi.Services.Pipeline;

/// <summary>
/// Excepción lanzada cuando un filtro del pipeline detecta un error irrecuperable.
/// 
/// Ejemplo: el JSON del modelo no cumple el schema → ValidacionMetamodeloFilter
/// lanza GeneracionException con los errores específicos.
/// 
/// El controller captura esta excepción y la convierte en una respuesta HTTP 400
/// con los errores de validación detallados.
/// </summary>
public class GeneracionException : Exception
{
    /// <summary>
    /// Errores detallados por campo o nodo.
    /// </summary>
    public List<ValidationError> Errores { get; }

    public GeneracionException(string mensaje, List<ValidationError>? errores = null)
        : base(mensaje)
    {
        Errores = errores ?? new List<ValidationError>();
    }

    public GeneracionException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
        Errores = new List<ValidationError>();
    }

    /// <summary>
    /// Constructor con mensaje, errores Y excepción interna.
    /// Usado cuando queremos propagar una excepción con contexto adicional.
    /// </summary>
    public GeneracionException(string mensaje, List<ValidationError> errores, Exception innerException)
        : base(mensaje, innerException)
    {
        Errores = errores ?? new List<ValidationError>();
    }
}
