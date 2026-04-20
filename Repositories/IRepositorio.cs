namespace GeneracionApi.Repositories;

/// <summary>
/// Contrato genérico de persistencia para MongoDB.
/// 
/// ¿Por qué genérico (&lt;T&gt;)?
/// Porque TODAS las entidades (Diagrama, ArtefactoGenerado, TraceLog, ConfigGeneracion)
/// necesitan las mismas operaciones básicas: leer, escribir, buscar, eliminar.
/// En vez de crear un repositorio por cada entidad, uno solo sirve para todas.
/// 
/// El repositorio es el ÚNICO lugar que conoce MongoDB.
/// Los servicios solo conocen esta interfaz, nunca MongoClient ni IMongoCollection.
/// </summary>
/// <typeparam name="T">Tipo de entidad de dominio (debe tener propiedad Id).</typeparam>
public interface IRepositorio<T> where T : class
{
    /// <summary>
    /// Obtiene una entidad por su ID.
    /// </summary>
    /// <param name="id">ID de la entidad (mapea a _id en MongoDB).</param>
    /// <returns>La entidad o null si no existe.</returns>
    Task<T?> ObtenerPorIdAsync(string id);

    /// <summary>
    /// Obtiene todas las entidades de la colección.
    /// Útil para listados pequeños. Para grandes volúmenes, usar BuscarAsync con filtros.
    /// </summary>
    /// <returns>Lista de todas las entidades.</returns>
    Task<List<T>> ObtenerTodosAsync();

    /// <summary>
    /// Busca entidades que cumplan un filtro.
    /// </summary>
    /// <param name="filtro">Expresión de filtro LINQ.
    /// Ejemplo: d => d.TipoDiagrama == "classDiagram"
    /// </param>
    /// <returns>Lista de entidades que cumplen el filtro.</returns>
    Task<List<T>> BuscarAsync(System.Linq.Expressions.Expression<Func<T, bool>> filtro);

    /// <summary>
    /// Inserta una nueva entidad en la colección.
    /// </summary>
    /// <param name="entidad">Entidad a insertar.</param>
    Task InsertarAsync(T entidad);

    /// <summary>
    /// Actualiza una entidad existente (reemplazo completo).
    /// </summary>
    /// <param name="id">ID de la entidad a actualizar.</param>
    /// <param name="entidad">Nuevos datos.</param>
    /// <returns>True si se actualizó, false si no existía.</returns>
    Task<bool> ActualizarAsync(string id, T entidad);

    /// <summary>
    /// Elimina una entidad por su ID.
    /// </summary>
    /// <param name="id">ID de la entidad a eliminar.</param>
    /// <returns>True si se eliminó, false si no existía.</returns>
    Task<bool> EliminarAsync(string id);

    /// <summary>
    /// Cuenta entidades que cumplan un filtro.
    /// Útil para paginación y estadísticas.
    /// </summary>
    /// <param name="filtro">Expresión de filtro (opcional, null = contar todo).</param>
    /// <returns>Número de entidades.</returns>
    Task<long> ContarAsync(System.Linq.Expressions.Expression<Func<T, bool>>? filtro = null);

    /// <summary>
    /// Obtiene la entidad ordenada descendentemente por el campo especificado.
    /// Útil para obtener el último registro según un criterio de ordenamiento.
    /// </summary>
    /// <param name="filtro">Expresión de filtro para buscar entidades.</param>
    /// <param name="ordenPorDesc">Expresión de ordenamiento descendente.
    /// Ejemplo: g => g.FechaCreacion</param>
    /// <returns>La primera entidad ordenada descendentemente o null si no hay resultados.</returns>
    Task<T?> ObtenerUltimaAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> filtro,
        System.Linq.Expressions.Expression<Func<T, object>> ordenPorDesc);
}
