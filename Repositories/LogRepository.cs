using GeneracionApi.Config;
using GeneracionApi.Domain;
using MongoDB.Driver;

namespace GeneracionApi.Repositories;

/// <summary>
/// Repositorio para TraceLog en MongoDB.
/// </summary>
public class LogRepository : IRepositorio<TraceLog>
{
    private readonly IMongoCollection<TraceLog> _coleccion;

    public LogRepository(IMongoClient mongoClient, MongoDbSettings settings)
    {
        var database = mongoClient.GetDatabase(settings.DatabaseName);
        _coleccion = database.GetCollection<TraceLog>("traceLogs");
    }

    public Task<TraceLog?> ObtenerPorIdAsync(string id)
    {
        return _coleccion.Find(l => l.Id == id).FirstOrDefaultAsync();
    }

    public Task<List<TraceLog>> ObtenerTodosAsync()
    {
        return _coleccion.Find(_ => true).ToListAsync();
    }

    public Task<List<TraceLog>> BuscarAsync(System.Linq.Expressions.Expression<Func<TraceLog, bool>> filtro)
    {
        return _coleccion.Find(filtro).ToListAsync();
    }

    public Task InsertarAsync(TraceLog entidad)
    {
        return _coleccion.InsertOneAsync(entidad);
    }

    public Task<bool> ActualizarAsync(string id, TraceLog entidad)
    {
        return _coleccion.ReplaceOneAsync(l => l.Id == id, entidad)
            .ContinueWith(t => t.Result.ModifiedCount > 0);
    }

    public Task<bool> EliminarAsync(string id)
    {
        return _coleccion.DeleteOneAsync(l => l.Id == id)
            .ContinueWith(t => t.Result.DeletedCount > 0);
    }

    public Task<long> ContarAsync(System.Linq.Expressions.Expression<Func<TraceLog, bool>>? filtro = null)
    {
        if (filtro == null)
            return _coleccion.CountDocumentsAsync(_ => true);
        
        return _coleccion.CountDocumentsAsync(filtro);
    }
}
