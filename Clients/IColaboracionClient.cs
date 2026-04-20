namespace GeneracionApi.Clients;

/// <summary>
/// Contrato para el cliente de colaboración del sistema Core.
/// 
/// Este cliente consume las APIs de colaboración del sistema Core externo.
/// </summary>
public interface IColaboracionClient
{
    /// <summary>
    /// Obtiene la información de colaboración de un proyecto desde Core.
    /// </summary>
    /// <param name="proyectoId">ID del proyecto.</param>
    /// <returns>Información de colaboración del proyecto.</returns>
    Task<ColaboracionResponse> ObtenerColaboracionAsync(string proyectoId);
}

/// <summary>
/// Respuesta de información de colaboración.
/// </summary>
public class ColaboracionResponse
{
    public string ProyectoId { get; set; } = string.Empty;
    public List<ColaboradorInfo> Colaboradores { get; set; } = new();
    public DateTime UltimaActualizacion { get; set; }
}

/// <summary>
/// Información de un colaborador.
/// </summary>
public class ColaboradorInfo
{
    public string UsuarioId { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
