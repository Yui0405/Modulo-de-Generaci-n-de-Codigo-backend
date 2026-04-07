using System.Linq.Expressions;
using System.Reflection;

namespace GeneracionApi.Repositories;

/// <summary>
/// Implementación en memoria de IRepositorio&lt;T&gt;.
/// Útil para testing, desarrollo, o cuando no se requiere persistencia real.
///
/// ¿Por qué Dictionary&lt;string, T&gt;?
/// Porque el ID es string (GUID en MongoDB). O(1) para operaciones por ID.
/// </summary>
/// <typeparam name="T">Tipo de entidad de dominio (debe tener propiedad Id).</typeparam>
public class InMemoryRepositorio<T> : IRepositorio<T> where T : class
{
    private readonly Dictionary<string, T> _almacen = new();
    private readonly object _lock = new();
    private readonly PropertyInfo? _propiedadId;

    public InMemoryRepositorio()
    {
        // Obtener la propiedad Id mediante reflection
        _propiedadId = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        
        if (_propiedadId == null)
        {
            throw new InvalidOperationException(
                $"El tipo {typeof(T).Name} debe tener una propiedad pública 'Id'");
        }
    }

    public Task<T?> ObtenerPorIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<T?>(null);

        lock (_lock)
        {
            _almacen.TryGetValue(id, out var entidad);
            return Task.FromResult(entidad);
        }
    }

    public Task<List<T>> ObtenerTodosAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_almacen.Values.ToList());
        }
    }

    public Task<List<T>> BuscarAsync(Expression<Func<T, bool>> filtro)
    {
        if (filtro == null)
            return Task.FromResult(new List<T>());

        // Compilar la expresión a un delegate para ejecución
        var predicado = filtro.Compile();

        lock (_lock)
        {
            var resultados = _almacen.Values.Where(predicado).ToList();
            return Task.FromResult(resultados);
        }
    }

    public Task InsertarAsync(T entidad)
    {
        if (entidad == null)
            throw new ArgumentNullException(nameof(entidad));

        lock (_lock)
        {
            // Generar ID si no existe
            var id = ObtenerId(entidad);
            if (string.IsNullOrWhiteSpace(id))
            {
                id = Guid.NewGuid().ToString();
                SetearId(entidad, id);
            }

            // Verificar que no exista otro con ese ID
            if (_almacen.ContainsKey(id))
            {
                throw new InvalidOperationException(
                    $"Ya existe una entidad con el ID {id}");
            }

            _almacen[id] = entidad;
        }

        return Task.CompletedTask;
    }

    public Task<bool> ActualizarAsync(string id, T entidad)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult(false);

        if (entidad == null)
            throw new ArgumentNullException(nameof(entidad));

        lock (_lock)
        {
            if (!_almacen.ContainsKey(id))
                return Task.FromResult(false);

            // Sincronizar el ID de la entidad con el ID proporcionado
            SetearId(entidad, id);
            _almacen[id] = entidad;
            return Task.FromResult(true);
        }
    }

    public Task<bool> EliminarAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult(false);

        lock (_lock)
        {
            return Task.FromResult(_almacen.Remove(id));
        }
    }

    public Task<long> ContarAsync(Expression<Func<T, bool>>? filtro = null)
    {
        lock (_lock)
        {
            if (filtro == null)
            {
                return Task.FromResult((long)_almacen.Count);
            }

            var predicado = filtro.Compile();
            var conteo = _almacen.Values.Count(predicado);
            return Task.FromResult((long)conteo);
        }
    }

    private string? ObtenerId(T entidad)
    {
        if (_propiedadId == null)
            return null;

        var valor = _propiedadId.GetValue(entidad);
        return valor as string;
    }

    private void SetearId(T entidad, string id)
    {
        _propiedadId?.SetValue(entidad, id);
    }
}
