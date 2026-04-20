namespace GeneracionApi.Config;

/// <summary>
/// Configuración para los endpoints de Core API.
/// Se mapea desde la sección "CoreApiSettings" en appsettings.json.
/// </summary>
public class CoreApiSettings
{
    /// <summary>
    /// URL base del sistema Core.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Timeout para las solicitudes HTTP a Core.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
