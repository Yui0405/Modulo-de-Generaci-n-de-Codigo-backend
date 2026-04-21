using System.Linq.Expressions;
using System.Text.Json;
using GeneracionApi.Domain;
using GeneracionApi.Repositories;
using GeneracionApi.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GeneracionApi.Tests.Services;

public class IntegracionServiceTests
{
    private readonly Mock<IRepositorio<Generacion>> _mockRepo;
    private readonly Mock<ILogger<IntegracionService>> _mockLogger;
    private readonly IntegracionService _service;

    public IntegracionServiceTests()
    {
        _mockRepo = new Mock<IRepositorio<Generacion>>();
        _mockLogger = new Mock<ILogger<IntegracionService>>();
        _service = new IntegracionService(_mockRepo.Object, _mockLogger.Object);
    }

    private static T GetProperty<T>(object obj, string propertyName)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            JsonSerializer.Serialize(obj));
        return dict != null && dict.TryGetValue(propertyName, out var value) 
            ? JsonSerializer.Deserialize<T>(value.GetRawText())!
            : default!;
    }

    [Fact]
    public async Task ConsultarAsync_ReturnsNull_WhenProyectoIdIsEmpty()
    {
        var result = await _service.ConsultarAsync("");
        Assert.Null(result);
    }

    [Fact]
    public async Task ConsultarAsync_ReturnsNull_WhenProyectoIdIsWhitespace()
    {
        var result = await _service.ConsultarAsync("   ");
        Assert.Null(result);
    }

    [Fact]
    public async Task ConsultarAsync_ReturnsSinIntegracion_WhenCountIsZero()
    {
        _mockRepo.Setup(r => r.ContarAsync(It.IsAny<Expression<Func<Generacion, bool>>>()))
            .ReturnsAsync(0);

        var result = await _service.ConsultarAsync("proj-1");

        Assert.NotNull(result);
        var estado = GetProperty<string>(result!, "EstadoIntegracion");
        Assert.Equal("sin_integracion", estado);
    }

    [Fact]
    public async Task ConsultarAsync_ReturnsError_WhenLastGenerationHasError()
    {
        var lastGen = new Generacion
        {
            Id = "gen-123",
            ProyectoId = "proj-1",
            SourceType = "classDiagram",
            Estado = "error",
            FechaCreacion = DateTime.UtcNow
        };

        _mockRepo.Setup(r => r.ContarAsync(It.IsAny<Expression<Func<Generacion, bool>>>()))
            .ReturnsAsync(5);

        _mockRepo.Setup(r => r.ObtenerUltimaAsync(
            It.IsAny<Expression<Func<Generacion, bool>>>(),
            It.IsAny<Expression<Func<Generacion, object>>>()))
            .ReturnsAsync(lastGen);

        var result = await _service.ConsultarAsync("proj-1");

        Assert.NotNull(result);
        var estado = GetProperty<string>(result!, "EstadoIntegracion");
        Assert.Equal("error", estado);
    }

    [Fact]
    public async Task ConsultarAsync_ReturnsActivo_WhenAtLeastOneSuccessful()
    {
        var lastGen = new Generacion
        {
            Id = "gen-123",
            ProyectoId = "proj-1",
            SourceType = "classDiagram",
            Estado = "exito",
            FechaCreacion = DateTime.UtcNow
        };

        _mockRepo.Setup(r => r.ContarAsync(It.IsAny<Expression<Func<Generacion, bool>>>()))
            .ReturnsAsync(3);

        _mockRepo.Setup(r => r.ObtenerUltimaAsync(
            It.IsAny<Expression<Func<Generacion, bool>>>(),
            It.IsAny<Expression<Func<Generacion, object>>>()))
            .ReturnsAsync(lastGen);

        var result = await _service.ConsultarAsync("proj-1");

        Assert.NotNull(result);
        var estado = GetProperty<string>(result!, "EstadoIntegracion");
        Assert.Equal("activo", estado);
    }
}