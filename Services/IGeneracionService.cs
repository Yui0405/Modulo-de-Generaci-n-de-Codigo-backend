using GeneracionApi.DTOs;

namespace GeneracionApi.Services;

/// <summary>
/// Contrato del servicio principal de generación de código.
/// 
/// Este servicio orquesta todo el flujo:
/// 1. Recibe el DTO del controller
/// 2. Crea el contexto de generación
/// 3. Ejecuta el pipeline
/// 4. Persiste resultados
/// 5. Retorna el DTO de respuesta
/// 
/// El controller solo llama a este servicio. No sabe del pipeline ni de MongoDB.
/// </summary>
public interface IGeneracionService
{
    /// <summary>
    /// Inicia una nueva generación de código.
    /// Ejecuta el pipeline completo y retorna los resultados.
    /// </summary>
    /// <param name="request">DTO con el modelo JSON y opciones.</param>
    /// <returns>DTO de respuesta con artefactos generados.</returns>
    Task<GeneracionResponseDto> GenerarAsync(GeneracionRequestDto request);

    /// <summary>
    /// Recupera una generación previamente ejecutada por su ID.
    /// </summary>
    /// <param name="generacionId">ID de la generación.</param>
    /// <returns>DTO con los artefactos o null si no existe.</returns>
    Task<GeneracionResponseDto?> ObtenerPorIdAsync(string generacionId);

    /// <summary>
    /// Obtiene el contenido de un archivo específico de una generación.
    /// </summary>
    /// <param name="generacionId">ID de la generación.</param>
    /// <param name="ruta">Ruta lógica del archivo dentro del artefacto.</param>
    /// <returns>Contenido del archivo o null si no existe.</returns>
    Task<string?> ObtenerArchivoAsync(string generacionId, string ruta);

    /// <summary>
    /// Genera un ZIP con todos los archivos de una generación.
    /// </summary>
    /// <param name="generacionId">ID de la generación.</param>
    /// <returns>Bytes del archivo ZIP o null si no existe la generación.</returns>
    Task<byte[]?> DescargarZipAsync(string generacionId);

    /// <summary>
    /// Regenera el código usando la misma configuración de una generación anterior.
    /// Ejecuta el pipeline nuevamente con los mismos datos.
    /// </summary>
    /// <param name="generacionId">ID de la generación original.</param>
    /// <returns>Nueva respuesta de generación o null si no existe la original.</returns>
    Task<GeneracionResponseDto?> RegenerarAsync(string generacionId);
}
