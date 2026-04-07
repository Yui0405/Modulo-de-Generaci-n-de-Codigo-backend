using System.Text.Json;

namespace GeneracionApi.Services.Pipeline.Filters;

/// <summary>
/// Filtro de validación del metamodelo.
/// 
/// Es la PRIMERA etapa del pipeline. Su trabajo es asegurar que
/// el JSON del modelo es válido ANTES de que cualquier otro filtro
/// intente procesarlo.
/// 
/// Analogía: es como el filtro de seguridad del aeropuerto.
/// Nadie pasa sin revisión. Si algo está mal, se rechaza de inmediato.
/// 
/// Validaciones que realiza:
/// 1. sourceType es un valor reconocido
/// 2. modelJson no está vacío
/// 3. modelJson es JSON válido (parseable)
/// 4. Estructura mínima según el tipo de modelo
/// 
/// Si alguna validación falla, agrega errores a contexto.ErroresValidacion.
/// Si hay errores al finalizar, el pipeline se detiene.
/// </summary>
public class ValidacionMetamodeloFilter : IFiltroGeneracion
{
    /// <summary>
    /// Tipos de modelo soportados.
    /// Si el frontend envía un tipo que no está aquí, se rechaza.
    /// </summary>
    private static readonly HashSet<string> TiposSoportados = new(StringComparer.OrdinalIgnoreCase)
    {
        "classDiagram",
        "erModel",
        "mvcModel"
    };

    public string Nombre => "ValidacionMetamodelo";

    public int Orden => 10;

    public Task EjecutarAsync(ContextoGeneracion contexto, Func<ContextoGeneracion, Task> siguiente)
    {
        // ──────────────────────────────────────────
        // Validación 1: sourceType existe y es soportado
        // ──────────────────────────────────────────
        var sourceType = contexto.Diagrama.TipoDiagrama;

        if (string.IsNullOrWhiteSpace(sourceType))
        {
            contexto.ErroresValidacion.Add(new ValidationError(
                Campo: "sourceType",
                Mensaje: "El tipo de modelo es obligatorio",
                Etapa: Nombre
            ));
        }
        else if (!TiposSoportados.Contains(sourceType))
        {
            contexto.ErroresValidacion.Add(new ValidationError(
                Campo: "sourceType",
                Mensaje: $"Tipo de modelo no soportado: '{sourceType}'. Valores válidos: {string.Join(", ", TiposSoportados)}",
                Etapa: Nombre
            ));
        }

        // ──────────────────────────────────────────
        // Validación 2: modelJson no está vacío
        // ──────────────────────────────────────────
        var modelJson = contexto.Diagrama.ModeloJson;

        if (string.IsNullOrWhiteSpace(modelJson))
        {
            contexto.ErroresValidacion.Add(new ValidationError(
                Campo: "modelJson",
                Mensaje: "El modelo JSON es obligatorio y no puede estar vacío",
                Etapa: Nombre
            ));

            // Si no hay JSON, no podemos validar más. Retornamos aquí.
            // El pipeline detectará los errores y responderá 400.
            return Task.CompletedTask;
        }

        // ──────────────────────────────────────────
        // Validación 3: modelJson es JSON válido
        // ──────────────────────────────────────────
        JsonDocument? documento;
        try
        {
            documento = JsonDocument.Parse(modelJson);
        }
        catch (JsonException ex)
        {
            contexto.ErroresValidacion.Add(new ValidationError(
                Campo: "modelJson",
                Mensaje: $"El modelo no es JSON válido: {ex.Message}",
                Etapa: Nombre
            ));
            return Task.CompletedTask;
        }

        // ──────────────────────────────────────────
        // Validación 4: Estructura mínima según tipo
        // ──────────────────────────────────────────
        if (contexto.ErroresValidacion.Count == 0)
        {
            ValidarEstructuraMinima(sourceType, documento, contexto);
        }

        documento.Dispose();

        // ──────────────────────────────────────────
        // Si hay errores, no continuamos con el pipeline
        // ──────────────────────────────────────────
        if (contexto.ErroresValidacion.Count > 0)
        {
            // No llamamos a siguiente() — el pipeline se detendrá
            // cuando detecte que hay errores.
            return Task.CompletedTask;
        }

        // ──────────────────────────────────────────
        // Todo válido → pasamos al siguiente filtro
        // ──────────────────────────────────────────
        return siguiente(contexto);
    }

    /// <summary>
    /// Valida la estructura mínima según el tipo de modelo.
    /// Cada tipo tiene sus propias reglas.
    /// </summary>
    private static void ValidarEstructuraMinima(
        string sourceType,
        JsonDocument documento,
        ContextoGeneracion contexto)
    {
        var root = documento.RootElement;

        switch (sourceType.ToLowerInvariant())
        {
            case "classdiagram":
                ValidarDiagramaClases(root, contexto);
                break;

            case "ermodel":
                ValidarModeloER(root, contexto);
                break;

            case "mvcscaffold":
                ValidarModeloMVC(root, contexto);
                break;

            default:
                // Tipo ya validado en Validación 1, no debería llegar aquí
                break;
        }
    }

    /// <summary>
    /// Valida que un diagrama de clases tenga al menos:
    /// - Un array "clases" con al menos un elemento
    /// - Cada clase tenga "nombre"
    /// </summary>
    private static void ValidarDiagramaClases(JsonElement root, ContextoGeneracion contexto)
    {
        if (!root.TryGetProperty("clases", out var clases))
        {
            contexto.ErroresValidacion.Add(new ValidationError(
                Campo: "clases",
                Mensaje: "El diagrama de clases debe contener un array 'clases'",
                Etapa: "ValidacionMetamodelo"
            ));
            return;
        }

        if (clases.ValueKind != JsonValueKind.Array)
        {
            contexto.ErroresValidacion.Add(new ValidationError(
                Campo: "clases",
                Mensaje: "'clases' debe ser un array",
                Etapa: "ValidacionMetamodelo"
            ));
            return;
        }

        if (clases.GetArrayLength() == 0)
        {
            contexto.ErroresValidacion.Add(new ValidationError(
                Campo: "clases",
                Mensaje: "El diagrama debe contener al menos una clase",
                Etapa: "ValidacionMetamodelo"
            ));
            return;
        }

        // Validar cada clase
        int index = 0;
        foreach (var clase in clases.EnumerateArray())
        {
            if (!clase.TryGetProperty("nombre", out var nombre) ||
                string.IsNullOrWhiteSpace(nombre.GetString()))
            {
                contexto.ErroresValidacion.Add(new ValidationError(
                    Campo: $"clases[{index}].nombre",
                    Mensaje: $"La clase en posición {index} debe tener un 'nombre' no vacío",
                    Etapa: "ValidacionMetamodelo"
                ));
            }
            index++;
        }
    }

    /// <summary>
    /// Valida que un modelo E-R tenga al menos:
    /// - Un array "entidades" con al menos un elemento
    /// - Cada entidad tenga "nombre"
    /// </summary>
    private static void ValidarModeloER(JsonElement root, ContextoGeneracion contexto)
    {
        if (!root.TryGetProperty("entidades", out var entidades))
        {
            contexto.ErroresValidacion.Add(new ValidationError(
                Campo: "entidades",
                Mensaje: "El modelo E-R debe contener un array 'entidades'",
                Etapa: "ValidacionMetamodelo"
            ));
            return;
        }

        if (entidades.ValueKind != JsonValueKind.Array || entidades.GetArrayLength() == 0)
        {
            contexto.ErroresValidacion.Add(new ValidationError(
                Campo: "entidades",
                Mensaje: "El modelo E-R debe contener al menos una entidad",
                Etapa: "ValidacionMetamodelo"
            ));
            return;
        }

        int index = 0;
        foreach (var entidad in entidades.EnumerateArray())
        {
            if (!entidad.TryGetProperty("nombre", out var nombre) ||
                string.IsNullOrWhiteSpace(nombre.GetString()))
            {
                contexto.ErroresValidacion.Add(new ValidationError(
                    Campo: $"entidades[{index}].nombre",
                    Mensaje: $"La entidad en posición {index} debe tener un 'nombre' no vacío",
                    Etapa: "ValidacionMetamodelo"
                ));
            }
            index++;
        }
    }

    /// <summary>
    /// Valida que un modelo MVC tenga al menos:
    /// - Un array "capas" o "componentes" con al menos un elemento
    /// </summary>
    private static void ValidarModeloMVC(JsonElement root, ContextoGeneracion contexto)
    {
        var propiedad = root.TryGetProperty("capas", out _) ? "capas" :
                        root.TryGetProperty("componentes", out _) ? "componentes" : null;

        if (propiedad == null)
        {
            contexto.ErroresValidacion.Add(new ValidationError(
                Campo: "capas/componentes",
                Mensaje: "El modelo MVC debe contener un array 'capas' o 'componentes'",
                Etapa: "ValidacionMetamodelo"
            ));
        }
    }
}
