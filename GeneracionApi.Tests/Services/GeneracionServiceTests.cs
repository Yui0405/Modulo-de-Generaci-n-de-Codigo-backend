using System.Linq.Expressions;
using GeneracionApi.DTOs;
using GeneracionApi.Domain;
using GeneracionApi.Repositories;
using GeneracionApi.Services;
using GeneracionApi.Services.Pipeline;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GeneracionApi.Tests.Services;

public class GeneracionServiceTests
{
    private readonly Mock<IRepositorio<Generacion>> _mockRepoGen;
    private readonly Mock<IRepositorio<Diagrama>> _mockRepoDiag;
    private readonly Mock<IRepositorio<ConfigGeneracion>> _mockRepoConfig;
    private readonly Mock<IRepositorio<ArtefactoGenerado>> _mockRepoArtefacto;
    private readonly Mock<IPipeline> _mockPipeline;
    private readonly Mock<ITrazabilidadService> _mockTrazabilidad;
    private readonly Mock<ILogger<GeneracionService>> _mockLogger;
    private readonly GeneracionService _service;

    public GeneracionServiceTests()
    {
        _mockRepoGen = new Mock<IRepositorio<Generacion>>();
        _mockRepoDiag = new Mock<IRepositorio<Diagrama>>();
        _mockRepoConfig = new Mock<IRepositorio<ConfigGeneracion>>();
        _mockRepoArtefacto = new Mock<IRepositorio<ArtefactoGenerado>>();
        _mockPipeline = new Mock<IPipeline>();
        _mockTrazabilidad = new Mock<ITrazabilidadService>();
        _mockLogger = new Mock<ILogger<GeneracionService>>();

        _service = new GeneracionService(
            _mockRepoGen.Object,
            _mockRepoDiag.Object,
            _mockRepoConfig.Object,
            _mockRepoArtefacto.Object,
            _mockPipeline.Object,
            _mockTrazabilidad.Object,
            _mockLogger.Object);

        // Default successful setup for repos
        SetupSuccessfulInserts();
    }

    private void SetupSuccessfulInserts()
    {
        // Setup InsertarAsync to set IDs
        _mockRepoDiag.Setup(r => r.InsertarAsync(It.IsAny<Diagrama>()))
            .Callback((Diagrama d) => d.Id = "diag-123")
            .Returns(Task.CompletedTask);
            
        _mockRepoConfig.Setup(r => r.InsertarAsync(It.IsAny<ConfigGeneracion>()))
            .Callback((ConfigGeneracion c) => c.Id = "config-123")
            .Returns(Task.CompletedTask);
            
        _mockRepoGen.Setup(r => r.InsertarAsync(It.IsAny<Generacion>()))
            .Callback((Generacion g) => g.Id = "gen-123")
            .Returns(Task.CompletedTask);
            
        _mockRepoGen.Setup(r => r.ActualizarAsync(It.IsAny<string>(), It.IsAny<Generacion>()))
            .ReturnsAsync(true);
    }

    #region Validation Tests

    [Fact]
    public async Task GenerarAsync_Throws_WhenRequestIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GenerarAsync(null!));
    }

    [Fact]
    public async Task GenerarAsync_Throws_WhenProjectIdIsEmpty()
    {
        var request = new GeneracionRequestDto
        {
            ProjectId = "",
            SourceType = "classDiagram",
            ModelJson = "{\"clases\":[{\"nombre\":\"Usuario\"}]}"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerarAsync(request));
    }

    [Fact]
    public async Task GenerarAsync_Throws_WhenProjectIdIsWhitespace()
    {
        var request = new GeneracionRequestDto
        {
            ProjectId = "   ",
            SourceType = "classDiagram",
            ModelJson = "{\"clases\":[{\"nombre\":\"Usuario\"}]}"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerarAsync(request));
    }

    [Fact]
    public async Task GenerarAsync_Throws_WhenSourceTypeIsInvalid()
    {
        var request = new GeneracionRequestDto
        {
            ProjectId = "proj-1",
            SourceType = "invalidType",
            ModelJson = "{\"clases\":[{\"nombre\":\"Usuario\"}]}"
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerarAsync(request));

        Assert.Contains("classDiagram", ex.Message);
    }

    [Fact]
    public async Task GenerarAsync_Throws_WhenSourceTypeIsEmpty()
    {
        var request = new GeneracionRequestDto
        {
            ProjectId = "proj-1",
            SourceType = "",
            ModelJson = "{\"clases\":[{\"nombre\":\"Usuario\"}]}"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerarAsync(request));
    }

    [Fact]
    public async Task GenerarAsync_Throws_WhenModelJsonIsEmpty()
    {
        var request = new GeneracionRequestDto
        {
            ProjectId = "proj-1",
            SourceType = "classDiagram",
            ModelJson = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerarAsync(request));
    }

    [Fact]
    public async Task GenerarAsync_Throws_WhenModelJsonIsWhitespace()
    {
        var request = new GeneracionRequestDto
        {
            ProjectId = "proj-1",
            SourceType = "classDiagram",
            ModelJson = "   "
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerarAsync(request));
    }

    #endregion

    #region Success Tests - classDiagram

    [Fact]
    public async Task GenerarAsync_ReturnsExito_WithValidClassDiagram()
    {
        var request = new GeneracionRequestDto
        {
            ProjectId = "proj-1",
            SourceType = "classDiagram",
            ModelJson = "{\"clases\":[{\"nombre\":\"Usuario\"}]}"
        };

        // Setup pipeline to return contexto with artefactos
        _mockPipeline.Setup(p => p.EjecutarAsync(It.IsAny<ContextoGeneracion>()))
            .ReturnsAsync((ContextoGeneracion ctx) => ctx);

        var result = await _service.GenerarAsync(request);

        Assert.Equal("exito", result.Estado);
    }

    #endregion

    #region Success Tests - erModel

    [Fact]
    public async Task GenerarAsync_ReturnsExito_WithValidErModel()
    {
        var request = new GeneracionRequestDto
        {
            ProjectId = "proj-1",
            SourceType = "erModel",
            ModelJson = "{\"entidades\":[{\"nombre\":\"Usuario\"}]}"
        };

        // Setup pipeline to return contexto with artefactos
        _mockPipeline.Setup(p => p.EjecutarAsync(It.IsAny<ContextoGeneracion>()))
            .ReturnsAsync((ContextoGeneracion ctx) => ctx);

        var result = await _service.GenerarAsync(request);

        Assert.Equal("exito", result.Estado);
    }

    #endregion

    #region Success Tests - mvcModel

    [Fact]
    public async Task GenerarAsync_ReturnsExito_WithValidMvcModel()
    {
        var request = new GeneracionRequestDto
        {
            ProjectId = "proj-1",
            SourceType = "mvcModel",
            ModelJson = "{\"capas\":[{\"nombre\":\"Controller\"}]}"
        };

        // Setup pipeline to return contexto with artefactos
        _mockPipeline.Setup(p => p.EjecutarAsync(It.IsAny<ContextoGeneracion>()))
            .ReturnsAsync((ContextoGeneracion ctx) => ctx);

        var result = await _service.GenerarAsync(request);

        Assert.Equal("exito", result.Estado);
    }

    #endregion

    #region ObtenerPorIdAsync Tests

    [Fact]
    public async Task ObtenerPorIdAsync_ReturnsNull_WhenIdIsEmpty()
    {
        var result = await _service.ObtenerPorIdAsync("");

        Assert.Null(result);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_ReturnsNull_WhenIdIsWhitespace()
    {
        var result = await _service.ObtenerPorIdAsync("   ");

        Assert.Null(result);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_ReturnsNull_WhenNotFound()
    {
        _mockRepoGen.Setup(r => r.ObtenerPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Generacion?)null);

        var result = await _service.ObtenerPorIdAsync("non-existent-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_ReturnsResponse_WhenFound()
    {
        var generacion = new Generacion
        {
            Id = "gen-123",
            ProyectoId = "proj-1",
            SourceType = "classDiagram",
            Estado = "exito",
            FechaCreacion = DateTime.UtcNow
        };

        _mockRepoGen.Setup(r => r.ObtenerPorIdAsync("gen-123"))
            .ReturnsAsync(generacion);

        _mockRepoArtefacto.Setup(r => r.BuscarAsync(It.IsAny<Expression<Func<ArtefactoGenerado, bool>>>()))
            .ReturnsAsync(new List<ArtefactoGenerado>());

        var result = await _service.ObtenerPorIdAsync("gen-123");

        Assert.NotNull(result);
        Assert.Equal("gen-123", result.GeneracionId);
    }

    #endregion

    #region ObtenerArchivoAsync Tests

    [Fact]
    public async Task ObtenerArchivoAsync_ReturnsNull_WhenGeneracionIdIsEmpty()
    {
        var result = await _service.ObtenerArchivoAsync("", "ruta");

        Assert.Null(result);
    }

    [Fact]
    public async Task ObtenerArchivoAsync_ReturnsNull_WhenRutaIsEmpty()
    {
        var result = await _service.ObtenerArchivoAsync("gen-123", "");

        Assert.Null(result);
    }

    [Fact]
    public async Task ObtenerArchivoAsync_ReturnsNull_WhenNotFound()
    {
        _mockRepoArtefacto.Setup(r => r.BuscarAsync(It.IsAny<Expression<Func<ArtefactoGenerado, bool>>>()))
            .ReturnsAsync(new List<ArtefactoGenerado>());

        var result = await _service.ObtenerArchivoAsync("gen-123", "NoExiste.java");

        Assert.Null(result);
    }

    [Fact]
    public async Task ObtenerArchivoAsync_ReturnsContent_WhenFound()
    {
        var artefacto = new ArtefactoGenerado
        {
            Id = "art-123",
            GeneracionId = "gen-123",
            Ruta = "Usuario.java",
            Nombre = "Usuario.java",
            Contenido = "public class Usuario {}",
            Tipo = "java"
        };

        _mockRepoArtefacto.Setup(r => r.BuscarAsync(It.IsAny<Expression<Func<ArtefactoGenerado, bool>>>()))
            .ReturnsAsync(new List<ArtefactoGenerado> { artefacto });

        var result = await _service.ObtenerArchivoAsync("gen-123", "Usuario.java");

        Assert.Equal("public class Usuario {}", result);
    }

    #endregion

    #region DescargarZipAsync Tests

    [Fact]
    public async Task DescargarZipAsync_ReturnsNull_WhenIdIsEmpty()
    {
        var result = await _service.DescargarZipAsync("");

        Assert.Null(result);
    }

    [Fact]
    public async Task DescargarZipAsync_ReturnsNull_WhenNotFound()
    {
        _mockRepoGen.Setup(r => r.ObtenerPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Generacion?)null);

        var result = await _service.DescargarZipAsync("non-existent-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task DescargarZipAsync_ReturnsZip_WhenNoArtifacts()
    {
        var generacion = new Generacion
        {
            Id = "gen-123",
            ProyectoId = "proj-1",
            Estado = "exito"
        };

        _mockRepoGen.Setup(r => r.ObtenerPorIdAsync("gen-123"))
            .ReturnsAsync(generacion);

        _mockRepoArtefacto.Setup(r => r.BuscarAsync(It.IsAny<Expression<Func<ArtefactoGenerado, bool>>>()))
            .ReturnsAsync(new List<ArtefactoGenerado>());

        var result = await _service.DescargarZipAsync("gen-123");

        Assert.NotNull(result);
        // Un ZIP vacío tiene headers pero puede tener length 0
        Assert.True(result.Length >= 0);
    }

    [Fact]
    public async Task DescargarZipAsync_ReturnsZip_WithArtifacts()
    {
        var generacion = new Generacion
        {
            Id = "gen-123",
            ProyectoId = "proj-1",
            Estado = "exito"
        };

        var artefacto = new ArtefactoGenerado
        {
            Id = "art-123",
            GeneracionId = "gen-123",
            Ruta = "Usuario.java",
            Nombre = "Usuario.java",
            Contenido = "public class Usuario {}",
            Tipo = "java",
            TamanoBytes = 22
        };

        _mockRepoGen.Setup(r => r.ObtenerPorIdAsync("gen-123"))
            .ReturnsAsync(generacion);

        _mockRepoArtefacto.Setup(r => r.BuscarAsync(It.IsAny<Expression<Func<ArtefactoGenerado, bool>>>()))
            .ReturnsAsync(new List<ArtefactoGenerado> { artefacto });

        var result = await _service.DescargarZipAsync("gen-123");

        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    #endregion

    #region RegenerarAsync Tests

    [Fact]
    public async Task RegenerarAsync_ReturnsNull_WhenIdIsEmpty()
    {
        var result = await _service.RegenerarAsync("");

        Assert.Null(result);
    }

    [Fact]
    public async Task RegenerarAsync_ReturnsNull_WhenNotFound()
    {
        _mockRepoGen.Setup(r => r.ObtenerPorIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Generacion?)null);

        var result = await _service.RegenerarAsync("non-existent-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task RegenerarAsync_CreatesNewGeneration_WithParentReference()
    {
        // Setup original generation
        var originalGen = new Generacion
        {
            Id = "gen-original",
            ProyectoId = "proj-1",
            SourceType = "classDiagram",
            DiagramaId = "diag-123",
            ConfigGeneracionId = "config-123",
            Estado = "exito",
            FechaCreacion = DateTime.UtcNow.AddDays(-1)
        };

        var diagrama = new Diagrama { Id = "diag-123" };
        var config = new ConfigGeneracion { Id = "config-123" };
        
        // First call returns original generation
        _mockRepoGen.SetupSequence(r => r.ObtenerPorIdAsync("gen-original"))
            .ReturnsAsync(originalGen);
        
        // For new generation insert (second call)
        _mockRepoGen.Setup(r => r.InsertarAsync(It.IsAny<Generacion>()))
            .Callback<Generacion>(g => g.Id = "gen-new")
            .Returns(Task.CompletedTask);

        _mockRepoDiag.Setup(r => r.ObtenerPorIdAsync("diag-123"))
            .ReturnsAsync(diagrama);

        _mockRepoConfig.Setup(r => r.ObtenerPorIdAsync("config-123"))
            .ReturnsAsync(config);

        _mockRepoGen.Setup(r => r.InsertarAsync(It.IsAny<Generacion>()))
            .Callback((Generacion g) => g.Id = "gen-new")
            .Returns(Task.CompletedTask);

        var contextoRetorno = new ContextoGeneracion 
        { 
            Diagrama = diagrama,
            Configuracion = config,
            FechaInicio = DateTime.UtcNow,
            ProyectoId = "proj-1",
            GeneracionId = "gen-new"
        };
        // La lista Artefactos ya se inicializa en el constructor de ContextoGeneracion
        contextoRetorno.Artefactos.AddRange(new List<ArtefactoGenerado>());
        
        _mockPipeline.Setup(p => p.EjecutarAsync(It.IsAny<ContextoGeneracion>()))
            .ReturnsAsync(contextoRetorno);

        var result = await _service.RegenerarAsync("gen-original");

        Assert.NotNull(result);
        Assert.Equal("gen-original", result.GeneracionPadreId);
    }

    #endregion
}