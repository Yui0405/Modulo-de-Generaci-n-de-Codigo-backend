using GeneracionApi.Config;
using GeneracionApi.Domain;
using MongoDB.Driver;

namespace GeneracionApi.Repositories;

/// <summary>
/// Repositorio para la entidad ConfigGeneracion en MongoDB.
/// </summary>
public class ConfigGeneracionRepository : IRepositorio<ConfigGeneracion>
{
    private readonly IMongoCollection<ConfigGeneracion> _coleccion;

    public ConfigGeneracionRepository(IMongoClient mongoClient, MongoDbSettings settings)
    {
        var database = mongoClient.GetDatabase(settings.DatabaseName);
        _coleccion = database.GetCollection<ConfigGeneracion>("configuraciones");
    }

    public Task<ConfigGeneracion?> ObtenerPorIdAsync(string id)
    {
        return _coleccion.Find(c => c.Id == id).FirstOrDefaultAsync();
    }

    public Task<List<ConfigGeneracion>> ObtenerTodosAsync()
    {
        return _coleccion.Find(_ => true).ToListAsync();
    }

    public Task<List<ConfigGeneracion>> BuscarAsync(System.Linq.Expressions.Expression<Func<ConfigGeneracion, bool>> filtro)
    {
        return _coleccion.Find(filtro).ToListAsync();
    }

    public Task InsertarAsync(ConfigGeneracion entidad)
    {
        return _coleccion.InsertOneAsync(entidad);
    }

    public Task<bool> ActualizarAsync(string id, ConfigGeneracion entidad)
    {
        return _coleccion.ReplaceOneAsync(c => c.Id == id, entidad)
            .ContinueWith(t => t.Result.ModifiedCount > 0);
    }

    public Task<bool> EliminarAsync(string id)
    {
        return _coleccion.DeleteOneAsync(c => c.Id == id)
            .ContinueWith(t => t.Result.DeletedCount > 0);
    }

    public Task<long> ContarAsync(System.Linq.Expressions.Expression<Func<ConfigGeneracion, bool>>? filtro = null)
    {
        if (filtro == null)
            return _coleccion.CountDocumentsAsync(_ => true);
        
        return _coleccion.CountDocumentsAsync(filtro);
    }
}
