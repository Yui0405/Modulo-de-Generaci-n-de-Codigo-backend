namespace GeneracionApi.Services.Pipeline;

/// <summary>
/// Contrato del orquestador del pipeline de generación.
/// 
/// El Pipeline es el "director de orquesta": no hace trabajo él mismo,
/// sino que coordina la ejecución de los filtros en orden.
/// 
/// Principio clave: el pipeline NO conoce los filtros concretos.
/// Recibe IEnumerable&lt;IFiltroGeneracion&gt; por inyección de dependencias.
/// Así, agregar un nuevo filtro es solo registrarlo en Program.cs.
/// </summary>
public interface IPipeline
{
    /// <summary>
    /// Ejecuta todos los filtros del pipeline en orden secuencial.
    /// </summary>
    /// <param name="contexto">Contexto que fluye por todos los filtros.</param>
    /// <returns>El mismo contexto con los resultados de la ejecución.</returns>
    /// <exception cref="GeneracionException">Si algún filtro falla.</exception>
    Task<ContextoGeneracion> EjecutarAsync(ContextoGeneracion contexto);
}
