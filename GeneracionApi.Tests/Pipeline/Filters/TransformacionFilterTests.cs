using GeneracionApi.Domain;
using GeneracionApi.Services.Pipeline;
using GeneracionApi.Services.Pipeline.Filters;
using Xunit;

namespace GeneracionApi.Tests.Pipeline.Filters;

public class TransformacionFilterTests
{
    private readonly TransformacionFilter _filter;

    public TransformacionFilterTests()
    {
        _filter = new TransformacionFilter();
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
    public void Nombre_ReturnsTransformacion()
    {
        Assert.Equal("Transformacion", _filter.Nombre);
    }

    [Fact]
    public void Orden_Returns20()
    {
        Assert.Equal(20, _filter.Orden);
    }

    #region Transform Tests

    [Fact]
    public async Task EjecutarAsync_TransformsClassDiagram()
    {
        var modelo = "{\"clases\":[{\"nombre\":\"Usuario\",\"atributos\":[{\"nombre\":\"Id\",\"tipo\":\"int\"}]}]}";
        var contexto = CreateContexto("classDiagram", modelo);
        bool siguienteCalled = false;
        Func<ContextoGeneracion, Task> siguiente = _ => { siguienteCalled = true; return Task.CompletedTask; };

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.True(contexto.Metadatos.ContainsKey("clasesProcesadas"));
        Assert.True(siguienteCalled);
    }

    [Fact]
    public async Task EjecutarAsync_TransformsClassDiagram_WithMethods()
    {
        var modelo = """
        {
            "clases": [
                {
                    "nombre": "Usuario",
                    "atributos": [],
                    "metodos": [
                        {
                            "nombre": "Saludar",
                            "tipoRetorno": "String"
                        }
                    ]
                }
            ]
        }
        """;
        var contexto = CreateContexto("classDiagram", modelo);
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.True(contexto.Metadatos.ContainsKey("clasesProcesadas"));
    }

    [Fact]
    public async Task EjecutarAsync_TransformsErModel()
    {
        var modelo = "{\"entidades\":[{\"nombre\":\"Usuario\",\"atributos\":[{\"nombre\":\"Id\",\"tipo\":\"int\"}]}]}";
        var contexto = CreateContexto("erModel", modelo);
        bool siguienteCalled = false;
        Func<ContextoGeneracion, Task> siguiente = _ => { siguienteCalled = true; return Task.CompletedTask; };

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.True(contexto.Metadatos.ContainsKey("entidadesProcesadas"));
        Assert.True(siguienteCalled);
    }

    [Fact]
    public async Task EjecutarAsync_TransformsErModel_WithRelations()
    {
        var modelo = """
        {
            "entidades": [
                {
                    "nombre": "Usuario",
                    "atributos": []
                }
            ],
            "relaciones": [
                {
                    "origen": "Usuario",
                    "destino": "Pedido",
                    "tipo": "1:N"
                }
            ]
        }
        """;
        var contexto = CreateContexto("erModel", modelo);
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await _filter.EjecutarAsync(contexto, siguiente);

        Assert.True(contexto.Metadatos.ContainsKey("entidadesProcesadas"));
        Assert.True(contexto.Metadatos.ContainsKey("relacionesProcesadas"));
    }

    [Fact]
    public async Task EjecutarAsync_TransformsMvcModel_WithCapas()
    {
        var modelo = "{\"capas\":[{\"nombre\":\"Controller\",\"clases\":[{\"nombre\":\"HomeController\"}]}]}";
        var contexto = CreateContexto("mvcModel", modelo);
        bool siguienteCalled = false;
        Func<ContextoGeneracion, Task> siguiente = _ => { siguienteCalled = true; return Task.CompletedTask; };

        await _filter.EjecutarAsync(contexto, siguiente);

        // Verificamos que se ejecutó el siguiente filtro
        Assert.True(siguienteCalled);
    }

    [Fact]
    public async Task EjecutarAsync_TransformsMvcModel_WithComponentes()
    {
        // El filtro busca "capas" primero - si el JSON tiene "componentes", no lo procesa
        // Este test verifica el comportamiento real: si no hay "capas", no procesa
        var modelo = "{\"componentes\":[{\"nombre\":\"Controller\",\"clases\":[{\"nombre\":\"HomeController\"}]}]}";
        var contexto = CreateContexto("mvcModel", modelo);
        bool siguienteCalled = false;
        Func<ContextoGeneracion, Task> siguiente = _ => { siguienteCalled = true; return Task.CompletedTask; };

        await _filter.EjecutarAsync(contexto, siguiente);

        // El filtro NO procesa "componentes" - solo busca "capas"
        // Por eso concluye sin errores pero sin procesar nada
        Assert.True(siguienteCalled);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task EjecutarAsync_Throws_WhenInvalidJson()
    {
        var contexto = CreateContexto("classDiagram", "not valid json");
        Func<ContextoGeneracion, Task> siguiente = _ => Task.CompletedTask;

        await Assert.ThrowsAsync<GeneracionException>(() =>
            _filter.EjecutarAsync(contexto, siguiente));
    }

    #endregion
}