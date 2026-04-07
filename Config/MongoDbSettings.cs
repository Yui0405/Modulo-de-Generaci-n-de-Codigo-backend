namespace GeneracionApi.Config;

/// <summary>
/// Clase para mapear la sección MongoDbSettings de appsettings.json.
/// Se inyecta como IOptions&lt;MongoDbSettings&gt; en los repositorios.
/// </summary>
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
