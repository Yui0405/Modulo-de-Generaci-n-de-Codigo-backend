using System.IO.Compression;
using GeneracionApi.DTOs;
using GeneracionApi.Domain;
using GeneracionApi.Repositories;
using GeneracionApi.Services.Pipeline;
using Microsoft.Extensions.Logging;

namespace GeneracionApi.Services;

/// <summary>
/// Servicio principal de orquestación de generación de código.
/// 
/// Coordina todo el flujo:
/// 1. Validación de request
/// 2. Creación de entidades (Diagrama, ConfigGeneracion, Generacion)
/// 3. Ejecución del pipeline
/// 4. Persistencia de artefactos
/// 5. Registro de trazabilidad
/// 6. Retorno de respuesta DTO
/// 
/// Este servicio es el UNICO punto de entrada para generación de código.
/// Los controllers llaman exclusivamente a este servicio.
/// </summary>
public class GeneracionService : IGeneracionService
{
    /// <summary>
    /// Tipos de SourceType válidos.
    /// </summary>
    private static readonly HashSet<string> TiposSourceTypeValidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "classDiagram",
        "erModel",
        "mvcModel"
    };

    private readonly IRepositorio<Generacion> _repositorioGeneracion;
    private readonly IRepositorio<Diagrama> _repositorioDiagrama;
    private readonly IRepositorio<ConfigGeneracion> _repositorioConfig;
    private readonly IRepositorio<ArtefactoGenerado> _repositorioArtefacto;
    private readonly IPipeline _pipeline;
    private readonly ITrazabilidadService _trazabilidadService;
    private readonly ILogger<GeneracionService> _logger;

    /// <summary>
    /// Constructor con inyección de dependencias.
    /// </summary>
    /// <param name="repositorioGeneracion">Repositorio para persistencia de Generacion.</param>
    /// <param name="repositorioDiagrama">Repositorio para persistencia de Diagrama.</param>
    /// <param name="repositorioConfig">Repositorio para persistencia de ConfigGeneracion.</param>
    /// <param name="repositorioArtefacto">Repositorio para persistencia de ArtefactoGenerado.</param>
    /// <param name="pipeline">Pipeline de ejecución de filtros.</param>
    /// <param name="trazabilidadService">Servicio de trazabilidad.</param>
    /// <param name="logger">Logger del servicio.</param>
    public GeneracionService(
        IRepositorio<Generacion> repositorioGeneracion,
        IRepositorio<Diagrama> repositorioDiagrama,
        IRepositorio<ConfigGeneracion> repositorioConfig,
        IRepositorio<ArtefactoGenerado> repositorioArtefacto,
        IPipeline pipeline,
        ITrazabilidadService trazabilidadService,
        ILogger<GeneracionService> logger)
    {
        _repositorioGeneracion = repositorioGeneracion ?? throw new ArgumentNullException(nameof(repositorioGeneracion));
        _repositorioDiagrama = repositorioDiagrama ?? throw new ArgumentNullException(nameof(repositorioDiagrama));
        _repositorioConfig = repositorioConfig ?? throw new ArgumentNullException(nameof(repositorioConfig));
        _repositorioArtefacto = repositorioArtefacto ?? throw new ArgumentNullException(nameof(repositorioArtefacto));
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _trazabilidadService = trazabilidadService ?? throw new ArgumentNullException(nameof(trazabilidadService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<GeneracionResponseDto> GenerarAsync(GeneracionRequestDto request)
    {
        // RN-001: Throw ArgumentNullException if request is null
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "La solicitud de generación no puede ser nula.");
        }

        // RN-002: Throw ArgumentException if request.ProjectId is empty
        if (string.IsNullOrWhiteSpace(request.ProjectId))
        {
            throw new ArgumentException("El ProjectId no puede estar vacío.", nameof(request.ProjectId));
        }

        // RN-003: Throw ArgumentException if SourceType not in ["classDiagram", "erModel", "mvcModel"]
        if (string.IsNullOrWhiteSpace(request.SourceType) || !TiposSourceTypeValidos.Contains(request.SourceType))
        {
            var tiposValidos = string.Join(", ", TiposSourceTypeValidos.OrderBy(t => t));
            throw new ArgumentException(
                $"El SourceType '{request.SourceType}' no es válido. Valores permitidos: {tiposValidos}",
                nameof(request.SourceType));
        }

        // RN-004: Throw ArgumentException if request.ModelJson is empty
        if (string.IsNullOrWhiteSpace(request.ModelJson))
        {
            throw new ArgumentException("El ModelJson no puede estar vacío.", nameof(request.ModelJson));
        }

        _logger.LogInformation("Iniciando generación para proyecto {ProjectId}, tipo {SourceType}", 
            request.ProjectId, request.SourceType);

        // Inferir lenguaje destino (default "java")
        var lenguajeDestino = "java";
        if (request.GenerationOptions != null && request.GenerationOptions.ContainsKey("lenguajeDestino"))
        {
            lenguajeDestino = request.GenerationOptions["lenguajeDestino"]?.ToString() ?? "java";
        }

        // Create Diagrama
        var diagrama = new Diagrama
        {
            TipoDiagrama = request.SourceType,
            Nombre = $"Diagrama-{DateTime.UtcNow:yyyyMMddHHmmss}",
            ModeloJson = request.ModelJson,
            ProyectoId = request.ProjectId,
            FechaCreacion = DateTime.UtcNow
        };

        // Create ConfigGeneracion
        var configGeneracion = new ConfigGeneracion
        {
            LenguajeDestino = lenguajeDestino,
            Opciones = request.GenerationOptions ?? new Dictionary<string, object>(),
            OutputOptions = request.OutputOptions ?? new Dictionary<string, object>(),
            FechaCreacion = DateTime.UtcNow
        };

        // Create Generacion ( Estado = "pendiente")
        var generacion = new Generacion
        {
            ProyectoId = request.ProjectId,
            SourceType = request.SourceType,
            Estado = "pendiente",
            FechaCreacion = DateTime.UtcNow
        };

        // Persist: Diagrama → ConfigGeneracion → Generacion (using InsertarAsync for each)
        await _repositorioDiagrama.InsertarAsync(diagrama);
        await _repositorioConfig.InsertarAsync(configGeneracion);
        await _repositorioGeneracion.InsertarAsync(generacion);

        // Update foreign keys
        generacion.DiagramaId = diagrama.Id ?? string.Empty;
        generacion.ConfigGeneracionId = configGeneracion.Id ?? string.Empty;
        await _repositorioGeneracion.ActualizarAsync(generacion.Id!, generacion);

        // Create ContextoGeneracion
        var contexto = new ContextoGeneracion
        {
            Diagrama = diagrama,
            Configuracion = configGeneracion,
            TraceabilityOptions = request.TraceabilityOptions ?? new Dictionary<string, object>(),
            ProyectoId = request.ProjectId,
            GeneracionId = generacion.Id!
        };

        // Register "inicio" event via ITrazabilidadService
        await _trazabilidadService.RegistrarEventoAsync(
            generacion.Id!,
            "inicio",
            $"Iniciada generación tipo {request.SourceType} para proyecto {request.ProjectId}");

        // Execute pipeline
        try
        {
            _logger.LogInformation("Ejecutando pipeline para generación {GeneracionId}", generacion.Id);
            await _pipeline.EjecutarAsync(contexto);

            // On success: Update Generacion
            generacion.Estado = "exito";
            generacion.FechaFin = DateTime.UtcNow;
            generacion.CantidadArtefactos = contexto.Artefactos.Count;
            await _repositorioGeneracion.ActualizarAsync(generacion.Id!, generacion);

            // Persist each ArtefactoGenerado
            foreach (var artefacto in contexto.Artefactos)
            {
                artefacto.GeneracionId = generacion.Id!;
                await _repositorioArtefacto.InsertarAsync(artefacto);
            }

            // Register "exito" event
            await _trazabilidadService.RegistrarEventoAsync(
                generacion.Id!,
                "exito",
                $"Generación exitosa. {contexto.Artefactos.Count} artefactos generados.");

            _logger.LogInformation("Generación {GeneracionId} completada exitosamente con {Cantidad} artefactos",
                generacion.Id, contexto.Artefactos.Count);

            // Return response
            return MapToResponseDto(generacion, contexto.Artefactos, contexto.FechaInicio);
        }
        catch (GeneracionException ex)
        {
            // On error: Update Generacion
            generacion.Estado = "error";
            generacion.MensajeError = ex.Message;
            generacion.FechaFin = DateTime.UtcNow;
            await _repositorioGeneracion.ActualizarAsync(generacion.Id!, generacion);

            // Register "error" event
            await _trazabilidadService.RegistrarEventoAsync(
                generacion.Id!,
                "error",
                $"Error en generación: {ex.Message}");

            _logger.LogError(ex, "Error en generación {GeneracionId}: {Mensaje}", generacion.Id, ex.Message);

            // Return response with error
            return new GeneracionResponseDto
            {
                GeneracionId = generacion.Id!,
                ProyectoId = generacion.ProyectoId,
                SourceType = generacion.SourceType,
                Estado = "error",
                Errores = new List<ValidacionErrorDto>
                {
                    new ValidacionErrorDto
                    {
                        Campo = "pipeline",
                        Mensaje = ex.Message
                    }
                },
                FechaGeneracion = generacion.FechaCreacion
            };
        }
    }

    /// <inheritdoc/>
    public async Task<GeneracionResponseDto?> ObtenerPorIdAsync(string generacionId)
    {
        // RN-009: Return null if not found (don't throw)
        if (string.IsNullOrWhiteSpace(generacionId))
        {
            return null;
        }

        _logger.LogDebug("Obteniendo generación por ID: {GeneracionId}", generacionId);

        // Get Generacion by ID
        var generacion = await _repositorioGeneracion.ObtenerPorIdAsync(generacionId);
        if (generacion == null)
        {
            _logger.LogDebug("Generación {GeneracionId} no encontrada", generacionId);
            return null;
        }

        // Get Artefactos by GeneracionId
        var artefactos = await _repositorioArtefacto.BuscarAsync(a => a.GeneracionId == generacionId);

        // Map to GeneracionResponseDto
        return MapToResponseDto(generacion, artefactos, generacion.FechaCreacion);
    }

    /// <inheritdoc/>
    public async Task<string?> ObtenerArchivoAsync(string generacionId, string ruta)
    {
        // RN-012: Return null if not found
        if (string.IsNullOrWhiteSpace(generacionId) || string.IsNullOrWhiteSpace(ruta))
        {
            return null;
        }

        _logger.LogDebug("Obteniendo archivo {Ruta} de generación {GeneracionId}", ruta, generacionId);

        // Search ArtefactoGenerado by GeneracionId + Ruta using BuscarAsync
        var artefactos = await _repositorioArtefacto.BuscarAsync(
            a => a.GeneracionId == generacionId && a.Ruta == ruta);

        var artefacto = artefactos.FirstOrDefault();
        if (artefacto == null)
        {
            _logger.LogDebug("Archivo {Ruta} no encontrado en generación {GeneracionId}", ruta, generacionId);
            return null;
        }

        // Return Contenido as string
        return artefacto.Contenido;
    }

    /// <inheritdoc/>
    public async Task<byte[]?> DescargarZipAsync(string generacionId)
    {
        // Get Generacion by ID
        if (string.IsNullOrWhiteSpace(generacionId))
        {
            return null;
        }

        _logger.LogDebug("Generando ZIP para generación {GeneracionId}", generacionId);

        var generacion = await _repositorioGeneracion.ObtenerPorIdAsync(generacionId);
        if (generacion == null)
        {
            _logger.LogDebug("Generación {GeneracionId} no encontrada para ZIP", generacionId);
            return null;
        }

        // Get all Artefactos by GeneracionId
        var artefactos = await _repositorioArtefacto.BuscarAsync(a => a.GeneracionId == generacionId);

        // RN-014: If no artefactos, still return valid ZIP (empty, not null)
        // Create ZIP in memory
        using var memoryStream = new MemoryStream();
        using var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true);

        // Add each artefacto using its Ruta as entry name
        foreach (var artefacto in artefactos)
        {
            var entry = zipArchive.CreateEntry(artefacto.Ruta, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(artefacto.Contenido);
        }

        _logger.LogInformation("ZIP generado para generación {GeneracionId} con {Cantidad} archivos",
            generacionId, artefactos.Count);

        // Return bytes
        return memoryStream.ToArray();
    }

    /// <inheritdoc/>
    public async Task<GeneracionResponseDto?> RegenerarAsync(string generacionId)
    {
        // Get original Generacion by ID
        if (string.IsNullOrWhiteSpace(generacionId))
        {
            return null;
        }

        _logger.LogInformation("Iniciando regeneración de generación {GeneracionId}", generacionId);

        var generacionOriginal = await _repositorioGeneracion.ObtenerPorIdAsync(generacionId);
        if (generacionOriginal == null)
        {
            _logger.LogDebug("Generación original {GeneracionId} no encontrada", generacionId);
            return null;
        }

        // Get Diagrama by DiagramaId
        var diagrama = await _repositorioDiagrama.ObtenerPorIdAsync(generacionOriginal.DiagramaId);
        if (diagrama == null)
        {
            _logger.LogError("Diagrama {DiagramaId} no encontrado para regeneración", generacionOriginal.DiagramaId);
            return null;
        }

        // Get ConfigGeneracion by ConfigGeneracionId
        var configGeneracion = await _repositorioConfig.ObtenerPorIdAsync(generacionOriginal.ConfigGeneracionId);
        if (configGeneracion == null)
        {
            _logger.LogError("ConfigGeneracion {ConfigGeneracionId} no encontrada para regeneración", 
                generacionOriginal.ConfigGeneracionId);
            return null;
        }

        // Create NEW Generacion with:
        // - Same ProyectoId, SourceType
        // - DiagramaId = original.DiagramaId (reuse, RN-018)
        // - ConfigGeneracionId = original.ConfigGeneracionId (reuse, RN-019)
        // - GeneracionPadreId = original.Id (RN-017)
        // - Estado = "pendiente"
        var nuevaGeneracion = new Generacion
        {
            ProyectoId = generacionOriginal.ProyectoId,
            SourceType = generacionOriginal.SourceType,
            DiagramaId = generacionOriginal.DiagramaId,
            ConfigGeneracionId = generacionOriginal.ConfigGeneracionId,
            GeneracionPadreId = generacionOriginal.Id,
            Estado = "pendiente",
            FechaCreacion = DateTime.UtcNow
        };

        // Persist NEW Generacion only (not Diagrama, not Config - reuse)
        await _repositorioGeneracion.InsertarAsync(nuevaGeneracion);

        // Create ContextoGeneracion with same Diagrama + Config
        var contexto = new ContextoGeneracion
        {
            Diagrama = diagrama,
            Configuracion = configGeneracion,
            TraceabilityOptions = new Dictionary<string, object>(),
            ProyectoId = generacionOriginal.ProyectoId,
            GeneracionId = nuevaGeneracion.Id!
        };

        // Register "inicio" event
        await _trazabilidadService.RegistrarEventoAsync(
            nuevaGeneracion.Id!,
            "inicio",
            $"Regeneración de generación {generacionOriginal.Id}, tipo {generacionOriginal.SourceType}");

        // Execute pipeline
        try
        {
            _logger.LogInformation("Ejecutando pipeline para regeneración {GeneracionId}", nuevaGeneracion.Id);
            await _pipeline.EjecutarAsync(contexto);

            // On success: update as in GenerarAsync
            nuevaGeneracion.Estado = "exito";
            nuevaGeneracion.FechaFin = DateTime.UtcNow;
            nuevaGeneracion.CantidadArtefactos = contexto.Artefactos.Count;
            await _repositorioGeneracion.ActualizarAsync(nuevaGeneracion.Id!, nuevaGeneracion);

            // Persist each ArtefactoGenerado
            foreach (var artefacto in contexto.Artefactos)
            {
                artefacto.GeneracionId = nuevaGeneracion.Id!;
                await _repositorioArtefacto.InsertarAsync(artefacto);
            }

            // Register "exito" event
            await _trazabilidadService.RegistrarEventoAsync(
                nuevaGeneracion.Id!,
                "exito",
                $"Regeneración exitosa. {contexto.Artefactos.Count} artefactos generados.");

            _logger.LogInformation("Regeneración {GeneracionId} completada exitosamente", nuevaGeneracion.Id);

            // Return new GeneracionResponseDto with GeneracionPadreId
            return MapToResponseDto(nuevaGeneracion, contexto.Artefactos, nuevaGeneracion.FechaCreacion);
        }
        catch (GeneracionException ex)
        {
            // On error: update as in GenerarAsync
            nuevaGeneracion.Estado = "error";
            nuevaGeneracion.MensajeError = ex.Message;
            nuevaGeneracion.FechaFin = DateTime.UtcNow;
            await _repositorioGeneracion.ActualizarAsync(nuevaGeneracion.Id!, nuevaGeneracion);

            // Register "error" event
            await _trazabilidadService.RegistrarEventoAsync(
                nuevaGeneracion.Id!,
                "error",
                $"Error en regeneración: {ex.Message}");

            _logger.LogError(ex, "Error en regeneración {GeneracionId}: {Mensaje}", nuevaGeneracion.Id, ex.Message);

            // Return response with error
            return new GeneracionResponseDto
            {
                GeneracionId = nuevaGeneracion.Id!,
                ProyectoId = nuevaGeneracion.ProyectoId,
                SourceType = nuevaGeneracion.SourceType,
                Estado = "error",
                GeneracionPadreId = generacionOriginal.Id,
                Errores = new List<ValidacionErrorDto>
                {
                    new ValidacionErrorDto
                    {
                        Campo = "pipeline",
                        Mensaje = ex.Message
                    }
                },
                FechaGeneracion = nuevaGeneracion.FechaCreacion
            };
        }
    }

    /// <summary>
    /// Mapea la entidad Generacion y sus artefactos al DTO de respuesta.
    /// </summary>
    /// <param name="generacion">Entidad de generación.</param>
    /// <param name="artefactos">Lista de artefactos generados.</param>
    /// <param name="fechaInicio">Fecha de inicio del pipeline.</param>
    /// <returns>DTO de respuesta mapeado.</returns>
    private GeneracionResponseDto MapToResponseDto(Generacion generacion, List<ArtefactoGenerado> artefactos, DateTime fechaInicio)
    {
        var duracionMs = (long)(DateTime.UtcNow - fechaInicio).TotalMilliseconds;

        return new GeneracionResponseDto
        {
            GeneracionId = generacion.Id ?? string.Empty,
            ProyectoId = generacion.ProyectoId,
            SourceType = generacion.SourceType,
            Estado = generacion.Estado,
            Archivos = artefactos.Select(a => new ArchivoGeneradoDto
            {
                Ruta = a.Ruta,
                Nombre = a.Nombre,
                Contenido = a.Contenido,
                Tipo = a.Tipo,
                TamanoBytes = a.TamanoBytes
            }).ToList(),
            CantidadArchivos = generacion.CantidadArtefactos,
            FechaGeneracion = generacion.FechaCreacion,
            GeneracionPadreId = generacion.GeneracionPadreId,
            Trazabilidad = new TrazabilidadMetadataDto
            {
                DuracionMs = duracionMs,
                FiltrosEjecutados = new List<string>() // El pipeline podría proporcionar esta info en contexto
            }
        };
    }
}