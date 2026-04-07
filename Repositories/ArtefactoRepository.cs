using GeneracionApi.Config;
using GeneracionApi.Domain;
using MongoDB.Driver;

namespace GeneracionApi.Repositories;

/// <summary>
/// Repositorio para la entidad ArtefactoGenerado en MongoDB.
/// </summary>
public class ArtefactoRepository : IRepositorio<ArtefactoGenerado>
{
    private readonly IMongoCollection<ArtefactoGenerado> _coleccion;

    public ArtefactoRepository(IMongoClient mongoClient, MongoDbSettings settings)
    {
        var database = mongoClient.GetDatabase(settings.DatabaseName);
        _coleccion = database.GetCollection<ArtefactoGenerado>("artefactos");
    }

    public Task<ArtefactoGenerado?> ObtenerPorIdAsync(string id)
    {
        return _coleccion.Find(a => a.Id == id).FirstOrDefaultAsync();
    }

    public Task<List<ArtefactoGenerado>> ObtenerTodosAsync()
    {
        return _coleccion.Find(_ => true).ToListAsync();
    }

    public Task<List<ArtefactoGenerado>> BuscarAsync(System.Linq.Expressions.Expression<Func<ArtefactoGenerado, bool>> filtro)
    {
        return _coleccion.Find(filtro).ToListAsync();
    }

    public Task InsertarAsync(ArtefactoGenerado entidad)
    {
        return _coleccion.InsertOneAsync(entidad);
    }

    public Task<bool> ActualizarAsync(string id, ArtefactoGenerado entidad)
    {
        return _coleccion.ReplaceOneAsync(a => a.Id == id, entidad)
            .ContinueWith(t => t.Result.ModifiedCount > 0);
    }

    public Task<bool> EliminarAsync(string id)
    {
        return _coleccion.DeleteOneAsync(a => a.Id == id)
            .ContinueWith(t => t.Result.DeletedCount > 0);
    }

    public Task<long> ContarAsync(System.Linq.Expressions.Expression<Func<ArtefactoGenerado, bool>>? filtro = null)
    {
        if (filtro == null)
            return _coleccion.CountDocumentsAsync(_ => true);
        
        return _coleccion.CountDocumentsAsync(filtro);
    }
}
