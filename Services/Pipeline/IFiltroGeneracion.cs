namespace GeneracionApi.Services.Pipeline;

/// <summary>
/// Contrato para cada filtro (etapa) del pipeline de generación.
/// 
/// El patrón Pipes & Filters funciona como una cadena de montaje:
/// cada filtro recibe un contexto, hace su trabajo, y lo pasa al siguiente.
/// 
/// Ejemplo de flujo:
///   [Request] → Validación → Transformación → Configuración → Generación → Auditoría → [Response]
/// 
/// Cada filtro puede:
///   - Modificar el contexto (agregar datos, transformar)
///   - Rechazar la solicitud (lanzar excepción si hay error)
///   - Pasar al siguiente filtro
/// </summary>
public interface IFiltroGeneracion
{
    /// <summary>
    /// Nombre descriptivo del filtro (para logs y debugging).
    /// Ejemplo: "ValidacionMetamodelo", "TransformacionJava"
    /// </summary>
    string Nombre { get; }

    /// <summary>
    /// Orden de ejecución en el pipeline. Se ejecutan de menor a mayor.
    ///   10 = Validación
    ///   20 = Transformación
    ///   30 = Configuración
    ///   40 = Generación
    ///   50 = Auditoría
    /// </summary>
    int Orden { get; }

    /// <summary>
    /// Ejecuta la lógica de este filtro sobre el contexto de generación.
    /// </summary>
    /// <param name="contexto">Contexto compartido que fluye por todo el pipeline.</param>
    /// <param name="siguiente">Delegado al siguiente filtro. Siempre llamarlo para continuar.</param>
    /// <returns>Task completado cuando el filtro termina su trabajo.</returns>
    /// <exception cref="GeneracionException">Lanzar si el filtro detecta un error irrecuperable.</exception>
    Task EjecutarAsync(ContextoGeneracion contexto, Func<ContextoGeneracion, Task> siguiente);
}
