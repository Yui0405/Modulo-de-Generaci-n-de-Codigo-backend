using GeneracionApi.Clients;
using GeneracionApi.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GeneracionApi.Extensions;

/// <summary>
/// Extensiones para el registro de clientes de Core en el contenedor de dependencias.
/// 
/// Proporciona integración "pluggable" para el sistema Core.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra los clientes de Core en el contenedor de dependencias.
    /// 
    /// Este método permite una integración modular ("plug and play") del sistema Core.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>La colección de servicios para encadenamiento.</returns>
    public static IServiceCollection AddCoreClients(this IServiceCollection services)
    {
        // Registrar HttpClient para clientes de Core
        // La configuración ya está disponible en IOptions<CoreApiSettings> porque se configuró en Program.cs
        services.AddHttpClient<ITrazabilidadClient, TrazabilidadClient>((serviceProvider, httpClient) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<CoreApiSettings>>().Value;
            httpClient.BaseAddress = new Uri(settings.BaseUrl);
            httpClient.Timeout = settings.Timeout;
        });

        services.AddHttpClient<IColaboracionClient, ColaboracionClient>((serviceProvider, httpClient) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<CoreApiSettings>>().Value;
            httpClient.BaseAddress = new Uri(settings.BaseUrl);
            httpClient.Timeout = settings.Timeout;
        });

        return services;
    }
}
