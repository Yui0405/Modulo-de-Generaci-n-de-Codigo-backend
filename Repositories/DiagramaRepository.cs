using GeneracionApi.Config;
using GeneracionApi.Domain;
using MongoDB.Driver;

namespace GeneracionApi.Repositories;

/// <summary>
/// Repositorio para la entidad Diagrama en MongoDB.
/// </summary>
public class DiagramaRepository : IRepositorio<Diagrama>
{
    private readonly IMongoCollection<Diagrama> _coleccion;

    public DiagramaRepository(IMongoClient mongoClient, MongoDbSettings settings)
    {
        var database = mongoClient.GetDatabase(settings.DatabaseName);
        _coleccion = database.GetCollection<Diagrama>("diagramas");
    }

    public Task<Diagrama?> ObtenerPorIdAsync(string id)
    {
        return _coleccion.Find(d => d.Id == id).FirstOrDefaultAsync();
    }

    public Task<List<Diagrama>> ObtenerTodosAsync()
    {
        return _coleccion.Find(_ => true).ToListAsync();
    }

    public Task<List<Diagrama>> BuscarAsync(System.Linq.Expressions.Expression<Func<Diagrama, bool>> filtro)
    {
        return _coleccion.Find(filtro).ToListAsync();
    }

    public Task InsertarAsync(Diagrama entidad)
    {
        return _coleccion.InsertOneAsync(entidad);
    }

    public Task<bool> ActualizarAsync(string id, Diagrama entidad)
    {
        return _coleccion.ReplaceOneAsync(d => d.Id == id, entidad)
            .ContinueWith(t => t.Result.ModifiedCount > 0);
    }

    public Task<bool> EliminarAsync(string id)
    {
        return _coleccion.DeleteOneAsync(d => d.Id == id)
            .ContinueWith(t => t.Result.DeletedCount > 0);
    }

    public Task<long> ContarAsync(System.Linq.Expressions.Expression<Func<Diagrama, bool>>? filtro = null)
    {
        if (filtro == null)
            return _coleccion.CountDocumentsAsync(_ => true);
        
        return _coleccion.CountDocumentsAsync(filtro);
    }

    public Task<Diagrama?> ObtenerUltimaAsync(
        System.Linq.Expressions.Expression<Func<Diagrama, bool>> filtro,
        System.Linq.Expressions.Expression<Func<Diagrama, object>> ordenPorDesc)
    {
        return _coleccion.Find(filtro)
            .SortByDescending(ordenPorDesc)
            .Limit(1)
            .FirstOrDefaultAsync();
    }
}
