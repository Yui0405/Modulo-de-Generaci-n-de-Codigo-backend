using GeneracionApi.Config;
using GeneracionApi.Domain;
using MongoDB.Driver;

namespace GeneracionApi.Repositories;

/// <summary>
/// Repositorio para la entidad Generacion en MongoDB.
/// </summary>
public class GeneracionRepository : IRepositorio<Generacion>
{
    private readonly IMongoCollection<Generacion> _coleccion;

    public GeneracionRepository(IMongoClient mongoClient, MongoDbSettings settings)
    {
        var database = mongoClient.GetDatabase(settings.DatabaseName);
        _coleccion = database.GetCollection<Generacion>("generaciones");
    }

    public Task<Generacion?> ObtenerPorIdAsync(string id)
    {
        return _coleccion.Find(g => g.Id == id).FirstOrDefaultAsync();
    }

    public Task<List<Generacion>> ObtenerTodosAsync()
    {
        return _coleccion.Find(_ => true).ToListAsync();
    }

    public Task<List<Generacion>> BuscarAsync(System.Linq.Expressions.Expression<Func<Generacion, bool>> filtro)
    {
        return _coleccion.Find(filtro).ToListAsync();
    }

    public Task InsertarAsync(Generacion entidad)
    {
        return _coleccion.InsertOneAsync(entidad);
    }

    public Task<bool> ActualizarAsync(string id, Generacion entidad)
    {
        return _coleccion.ReplaceOneAsync(g => g.Id == id, entidad)
            .ContinueWith(t => t.Result.ModifiedCount > 0);
    }

    public Task<bool> EliminarAsync(string id)
    {
        return _coleccion.DeleteOneAsync(g => g.Id == id)
            .ContinueWith(t => t.Result.DeletedCount > 0);
    }

    public Task<long> ContarAsync(System.Linq.Expressions.Expression<Func<Generacion, bool>>? filtro = null)
    {
        if (filtro == null)
            return _coleccion.CountDocumentsAsync(_ => true);
        
        return _coleccion.CountDocumentsAsync(filtro);
    }

public Task<Generacion?> ObtenerUltimaAsync(
        System.Linq.Expressions.Expression<Func<Generacion, bool>> filtro,
        System.Linq.Expressions.Expression<Func<Generacion, object>> ordenPorDesc)
    {
        return _coleccion.Find(filtro)
            .SortByDescending(ordenPorDesc)
            .Limit(1)
            .FirstOrDefaultAsync();
    }
}
