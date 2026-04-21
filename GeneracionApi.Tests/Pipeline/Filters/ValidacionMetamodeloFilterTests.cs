using GeneracionApi.Domain;
using GeneracionApi.Services.Pipeline;
using GeneracionApi.Services.Pipeline.Filters;
using Xunit;

namespace GeneracionApi.Tests.Pipeline.Filters;

public class ValidacionMetamodeloFilterTests
{
    private readonly ValidacionMetamodeloFilter _filter;

    public ValidacionMetamodeloFilterTests()
    {
        _filter = new ValidacionMetamodeloFilter();
    }

    private static ContextoGeneracion CreateContexto(string tipoDiagrama, string modeloJson)
    {
        return new ContextoGeneracion
        {
            Diagrama = new Diagrama
            {
                TipoDiagrama = tipoDiagrama,
                ModeloJson = modeloJson,
                ProyectoId = "test-project"
            },
            Configuracion = new ConfigGeneracion
            {
                LenguajeDestino = "java"
            }
        };
    }

    [Fact]
    public void Nombre_ReturnsValidacionMetamodelo()
    {
        Assert.Equal("ValidacionMetamodelo", _filter.Nombre);
    }

    [Fact]
    public void Orden_Returns10()
    {
        Assert.Equal(10, _filter.Orden);
    }

    #region Empty/Invalid sourceType Tests

    [Fact]
    public async Task EjecutarAsync_AddsError_WhenSourceTypeIsEmpty()
    {
        var contexto = CreateContexto("", "{\"clases\":[{\"nombre\":\"Usuario\"}]}");
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.NotEmpty(contexto.ErroresValidacion);
        Assert.Contains(contexto.ErroresValidacion, e => e.Campo == "sourceType");
    }

    [Fact]
    public async Task EjecutarAsync_AddsError_WhenSourceTypeIsInvalid()
    {
        var contexto = CreateContexto("invalidType", "{\"clases\":[{\"nombre\":\"Usuario\"}]}");
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.NotEmpty(contexto.ErroresValidacion);
        Assert.Contains(contexto.ErroresValidacion, e => e.Campo == "sourceType");
    }

    #endregion

    #region Empty/Invalid modelJson Tests

    [Fact]
    public async Task EjecutarAsync_AddsError_WhenModelJsonIsEmpty()
    {
        var contexto = CreateContexto("classDiagram", "");
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.NotEmpty(contexto.ErroresValidacion);
        Assert.Contains(contexto.ErroresValidacion, e => e.Campo == "modelJson");
    }

    [Fact]
    public async Task EjecutarAsync_AddsError_WhenModelJsonIsInvalidJson()
    {
        var contexto = CreateContexto("classDiagram", "not valid json {");
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.NotEmpty(contexto.ErroresValidacion);
        Assert.Contains(contexto.ErroresValidacion, e => e.Campo == "modelJson");
    }

    #endregion

    #region Valid classDiagram Tests

    [Fact]
    public async Task EjecutarAsync_PassesValidation_WhenClassDiagramIsValid()
    {
        var contexto = CreateContexto("classDiagram", "{\"clases\":[{\"nombre\":\"Usuario\"}]}");
        bool siguienteCalled = false;
        Func<ContextoGeneracion, Task> siguiente = _ => { siguienteCalled = true; return Task.CompletedTask; };

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.Empty(contexto.ErroresValidacion);
        Assert.True(siguienteCalled);
    }

    [Fact]
    public async Task EjecutarAsync_PassesValidation_WithClassAttributes()
    {
        var modelo = "{\"clases\":[{\"nombre\":\"Usuario\",\"atributos\":[{\"nombre\":\"Id\",\"tipo\":\"int\"}]}]}";
        var contexto = CreateContexto("classDiagram", modelo);
        bool siguienteCalled = false;
        Func<ContextoGeneracion, Task> siguiente = _ => { siguienteCalled = true; return Task.CompletedTask; };

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.Empty(contexto.ErroresValidacion);
        Assert.True(siguienteCalled);
    }

    #endregion

    #region Valid erModel Tests

    [Fact]
    public async Task EjecutarAsync_PassesValidation_WhenErModelIsValid()
    {
        var contexto = CreateContexto("erModel", "{\"entidades\":[{\"nombre\":\"Usuario\"}]}");
        bool siguienteCalled = false;
        Func<ContextoGeneracion, Task> siguiente = _ => { siguienteCalled = true; return Task.CompletedTask; };

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.Empty(contexto.ErroresValidacion);
        Assert.True(siguienteCalled);
    }

    #endregion

    #region Structure Validation Tests

    [Fact]
    public async Task EjecutarAsync_AddsError_WhenClassDiagramHasNoClasesArray()
    {
        var contexto = CreateContexto("classDiagram", "{\"nombre\":\"Usuario\"}");
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.NotEmpty(contexto.ErroresValidacion);
        Assert.Contains(contexto.ErroresValidacion, e => e.Campo == "clases");
    }

    [Fact]
    public async Task EjecutarAsync_AddsError_WhenClassDiagramHasEmptyClases()
    {
        var contexto = CreateContexto("classDiagram", "{\"clases\":[]}");
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.NotEmpty(contexto.ErroresValidacion);
        Assert.Contains(contexto.ErroresValidacion, e => e.Campo == "clases");
    }

    [Fact]
    public async Task EjecutarAsync_AddsError_WhenClassDiagramClassHasNoNombre()
    {
        var contexto = CreateContexto("classDiagram", "{\"clases\":[{\"atributos\":[]}]}");
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.NotEmpty(contexto.ErroresValidacion);
        Assert.Contains(contexto.ErroresValidacion, e => e.Campo.Contains("nombre"));
    }

    [Fact]
    public async Task EjecutarAsync_AddsError_WhenErModelHasNoEntidadesArray()
    {
        var contexto = CreateContexto("erModel", "{\"nombre\":\"Usuario\"}");
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.NotEmpty(contexto.ErroresValidacion);
        Assert.Contains(contexto.ErroresValidacion, e => e.Campo == "entidades");
    }

    [Fact]
    public async Task EjecutarAsync_AddsError_WhenErModelHasEmptyEntidades()
    {
        var contexto = CreateContexto("erModel", "{\"entidades\":[]}");
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.NotEmpty(contexto.ErroresValidacion);
        Assert.Contains(contexto.ErroresValidacion, e => e.Campo == "entidades");
    }

    #endregion
}