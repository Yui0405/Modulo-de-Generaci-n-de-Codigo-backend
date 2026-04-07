using System.Text.Json;

namespace GeneracionApi.Services.Pipeline.Filters;

/// <summary>
/// Filtro de transformación intermedia.
/// 
/// Convierte el JSON del modelo (tal como lo envía el frontend) a una
/// representación interna más procesable que se guarda en contexto.Metadatos.
/// 
/// Este filtro NO genera código. Solo prepara los datos para que
/// GeneracionArtefactosFilter pueda trabajar fácilmente.
/// 
/// Analogía: es como un traductor. El frontend habla en "JSON del usuario",
/// el generador habla en "estructuras internas". Este filtro traduce entre ambos.
/// </summary>
public class TransformacionFilter : IFiltroGeneracion
{
    public string Nombre => "Transformacion";

    public int Orden => 20;

    public Task EjecutarAsync(ContextoGeneracion contexto, Func<ContextoGeneracion, Task> siguiente)
    {
        var sourceType = contexto.Diagrama.TipoDiagrama.ToLowerInvariant();
        var modelJson = contexto.Diagrama.ModeloJson;

        try
        {
            using var documento = JsonDocument.Parse(modelJson);
            var root = documento.RootElement;

            switch (sourceType)
            {
                case "classdiagram":
                    TransformarDiagramaClases(root, contexto);
                    break;

                case "ermodel":
                    TransformarModeloER(root, contexto);
                    break;

                case "mvcscaffold":
                    TransformarModeloMVC(root, contexto);
                    break;
            }

            return siguiente(contexto);
        }
        catch (Exception ex)
        {
            throw new GeneracionException(
                $"Error en transformación: {ex.Message}",
                new List<ValidationError>
                {
                    new("modelJson", $"No se pudo transformar el modelo: {ex.Message}", Nombre)
                });
        }
    }

    /// <summary>
    /// Transforma un diagrama de clases a estructura interna.
    /// </summary>
    private static void TransformarDiagramaClases(JsonElement root, ContextoGeneracion contexto)
    {
        var clasesProcesadas = new List<object>();

        if (root.TryGetProperty("clases", out var clases))
        {
            foreach (var clase in clases.EnumerateArray())
            {
                var nombreClase = clase.GetProperty("nombre").GetString() ?? "SinNombre";

                // Procesar atributos
                var atributos = new List<object>();
                if (clase.TryGetProperty("atributos", out var attrs))
                {
                    foreach (var attr in attrs.EnumerateArray())
                    {
                        atributos.Add(new
                        {
                            Nombre = attr.GetProperty("nombre").GetString(),
                            Tipo = attr.TryGetProperty("tipo", out var t) ? t.GetString() : "Object",
                            EsPrivado = true
                        });
                    }
                }

                // Procesar métodos
                var metodos = new List<object>();
                if (clase.TryGetProperty("metodos", out var methods))
                {
                    foreach (var metodo in methods.EnumerateArray())
                    {
                        metodos.Add(new
                        {
                            Nombre = metodo.GetProperty("nombre").GetString(),
                            TipoRetorno = metodo.TryGetProperty("tipoRetorno", out var tr) ? tr.GetString() : "void",
                            Parametros = metodo.TryGetProperty("parametros", out var p) ? p.ToString() : "[]"
                        });
                    }
                }

                // Herencia e interfaces
                var heredaDe = clase.TryGetProperty("heredaDe", out var hd) ? hd.GetString() : null;
                var implementa = new List<string>();
                if (clase.TryGetProperty("implementa", out var impl))
                {
                    foreach (var i in impl.EnumerateArray())
                    {
                        implementa.Add(i.GetString() ?? "");
                    }
                }

                clasesProcesadas.Add(new
                {
                    Nombre = nombreClase,
                    NombreArchivo = $"{nombreClase}.java",
                    RutaPaquete = "modelo",
                    Atributos = atributos,
                    Metodos = metodos,
                    HeredaDe = heredaDe,
                    Implementa = implementa
                });
            }
        }

        contexto.Metadatos["clasesProcesadas"] = clasesProcesadas;
    }

    /// <summary>
    /// Transforma un modelo E-R a estructura interna.
    /// </summary>
    private static void TransformarModeloER(JsonElement root, ContextoGeneracion contexto)
    {
        var entidadesProcesadas = new List<object>();

        if (root.TryGetProperty("entidades", out var entidades))
        {
            foreach (var entidad in entidades.EnumerateArray())
            {
                var nombreEntidad = entidad.GetProperty("nombre").GetString() ?? "SinNombre";

                // Procesar atributos/columnas
                var columnas = new List<object>();
                if (entidad.TryGetProperty("atributos", out var attrs))
                {
                    foreach (var attr in attrs.EnumerateArray())
                    {
                        columnas.Add(new
                        {
                            Nombre = attr.GetProperty("nombre").GetString(),
                            TipoSql = attr.TryGetProperty("tipo", out var t) ? t.GetString() : "VARCHAR(255)",
                            EsPrimaryKey = attr.TryGetProperty("esPk", out var pk) && pk.GetBoolean(),
                            EsForeignKey = attr.TryGetProperty("esFk", out var fk) && fk.GetBoolean(),
                            Referencia = attr.TryGetProperty("referencia", out var r) ? r.GetString() : null,
                            NotNull = attr.TryGetProperty("notNull", out var nn) && nn.GetBoolean(),
                            Unique = attr.TryGetProperty("unique", out var u) && u.GetBoolean()
                        });
                    }
                }

                entidadesProcesadas.Add(new
                {
                    Nombre = nombreEntidad,
                    NombreTabla = NombreATabla(nombreEntidad),
                    Columnas = columnas
                });
            }
        }

        // Procesar relaciones si existen
        var relaciones = new List<object>();
        if (root.TryGetProperty("relaciones", out var rels))
        {
            foreach (var rel in rels.EnumerateArray())
            {
                relaciones.Add(new
                {
                    Origen = rel.TryGetProperty("origen", out var o) ? o.GetString() : "",
                    Destino = rel.TryGetProperty("destino", out var d) ? d.GetString() : "",
                    Tipo = rel.TryGetProperty("tipo", out var t) ? t.GetString() : "1:N"
                });
            }
        }

        contexto.Metadatos["entidadesProcesadas"] = entidadesProcesadas;
        contexto.Metadatos["relacionesProcesadas"] = relaciones;
    }

    /// <summary>
    /// Transforma un modelo MVC a estructura interna.
    /// </summary>
    private static void TransformarModeloMVC(JsonElement root, ContextoGeneracion contexto)
    {
        var capasProcesadas = new List<object>();

        var propiedad = root.TryGetProperty("capas", out var c) ? c :
                        root.TryGetProperty("componentes", out var comp) ? comp : default;

        if (propiedad.ValueKind == JsonValueKind.Array)
        {
            foreach (var capa in propiedad.EnumerateArray())
            {
                var nombreCapa = capa.GetProperty("nombre").GetString() ?? "SinNombre";

                var clasesCapa = new List<object>();
                if (capa.TryGetProperty("clases", out var clases))
                {
                    foreach (var clase in clases.EnumerateArray())
                    {
                        clasesCapa.Add(new
                        {
                            Nombre = clase.GetProperty("nombre").GetString(),
                            Tipo = clase.TryGetProperty("tipo", out var t) ? t.GetString() : "class"
                        });
                    }
                }

                capasProcesadas.Add(new
                {
                    Nombre = nombreCapa,
                    Directorio = nombreCapa.ToLowerInvariant(),
                    Clases = clasesCapa
                });
            }
        }

        contexto.Metadatos["capasProcesadas"] = capasProcesadas;
    }

    /// <summary>
    /// Convierte un nombre PascalCase a nombre de tabla SQL (snake_case plural).
    /// Ejemplo: "DetallePedido" → "detalle_pedido"
    /// </summary>
    private static string NombreATabla(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return nombre;

        var resultado = new System.Text.StringBuilder();
        for (int i = 0; i < nombre.Length; i++)
        {
            if (char.IsUpper(nombre[i]) && i > 0)
            {
                resultado.Append('_');
            }
            resultado.Append(char.ToLower(nombre[i]));
        }
        return resultado.ToString();
    }
}
