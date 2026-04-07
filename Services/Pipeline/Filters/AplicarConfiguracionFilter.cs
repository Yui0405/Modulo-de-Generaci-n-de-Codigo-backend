namespace GeneracionApi.Services.Pipeline.Filters;

/// <summary>
/// Filtro de aplicación de configuración.
/// 
/// Toma las opciones del usuario (ConfigGeneracion) y las aplica
/// sobre la estructura intermedia creada por TransformacionFilter.
/// 
/// Ejemplos de configuraciones que aplica:
/// - Java: incluirGettersSetters, incluirConstructores, paqueteBase, convenciónNombres
/// - SQL: dialecto (MySQL/PostgreSQL), incluirDropIfExists, incluirComentarios
/// - MVC: framework, gestor de dependencias
/// 
/// Este filtro NO genera código. Solo modifica la estructura intermedia
/// en contexto.Metadatos según las opciones del usuario.
/// 
/// Analogía: es como un "configurador". Ya tienes las piezas del mueble,
/// pero decides si quieres tornillos visibles o invisibles, barniz o pintura.
/// </summary>
public class AplicarConfiguracionFilter : IFiltroGeneracion
{
    public string Nombre => "AplicarConfiguracion";

    public int Orden => 30;

    public Task EjecutarAsync(ContextoGeneracion contexto, Func<ContextoGeneracion, Task> siguiente)
    {
        var opciones = contexto.Configuracion.Opciones;
        var outputOptions = contexto.Configuracion.OutputOptions;
        var sourceType = contexto.Diagrama.TipoDiagrama.ToLowerInvariant();

        // Guardar las opciones normalizadas para uso posterior
        var opcionesAplicadas = new Dictionary<string, object>();

        switch (sourceType)
        {
            case "classdiagram":
                AplicarConfiguracionJava(opciones, outputOptions, opcionesAplicadas, contexto);
                break;

            case "ermodel":
                AplicarConfiguracionSQL(opciones, outputOptions, opcionesAplicadas, contexto);
                break;

            case "mvcscaffold":
                AplicarConfiguracionMVC(opciones, outputOptions, opcionesAplicadas, contexto);
                break;
        }

        // Guardar las opciones aplicadas para referencia posterior
        contexto.Metadatos["opcionesAplicadas"] = opcionesAplicadas;

        return siguiente(contexto);
    }

    /// <summary>
    /// Aplica configuraciones específicas para generación Java.
    /// </summary>
    private static void AplicarConfiguracionJava(
        Dictionary<string, object> opciones,
        Dictionary<string, object> outputOptions,
        Dictionary<string, object> aplicadas,
        ContextoGeneracion contexto)
    {
        // Obtener opciones con valores por defecto
        var incluirGetters = ObtenerBool(opciones, "incluirGettersSetters", true);
        var incluirConstructores = ObtenerBool(opciones, "incluirConstructores", true);
        var paqueteBase = ObtenerString(opciones, "paqueteBase", "com.organizacion.modelo");
        var convencionNombres = ObtenerString(opciones, "convencionNombres", "camelCase");
        var incluirComentarios = ObtenerBool(outputOptions, "incluirComentarios", true);
        var incluirTrazabilidad = ObtenerBool(outputOptions, "incluirTrazabilidad", false);

        aplicadas["incluirGettersSetters"] = incluirGetters;
        aplicadas["incluirConstructores"] = incluirConstructores;
        aplicadas["paqueteBase"] = paqueteBase;
        aplicadas["convencionNombres"] = convencionNombres;
        aplicadas["incluirComentarios"] = incluirComentarios;
        aplicadas["incluirTrazabilidad"] = incluirTrazabilidad;
        aplicadas["extension"] = ".java";

        // Aplicar sobre las clases procesadas si existen
        if (contexto.Metadatos.TryGetValue("clasesProcesadas", out var clasesObj) &&
            clasesObj is List<object> clases)
        {
            // Actualizar la ruta del paquete en cada clase
            // (esto se usará en GeneracionArtefactosFilter)
            aplicadas["rutaPaquete"] = paqueteBase.Replace('.', '/');
        }
    }

    /// <summary>
    /// Aplica configuraciones específicas para generación SQL.
    /// </summary>
    private static void AplicarConfiguracionSQL(
        Dictionary<string, object> opciones,
        Dictionary<string, object> outputOptions,
        Dictionary<string, object> aplicadas,
        ContextoGeneracion contexto)
    {
        var dialecto = ObtenerString(opciones, "dialecto", "mysql").ToLowerInvariant();
        var incluirDrop = ObtenerBool(opciones, "incluirDropIfExists", false);
        var incluirComentarios = ObtenerBool(outputOptions, "incluirComentarios", true);

        aplicadas["dialecto"] = dialecto;
        aplicadas["incluirDropIfExists"] = incluirDrop;
        aplicadas["incluirComentarios"] = incluirComentarios;
        aplicadas["extension"] = ".sql";

        // Mapeo de tipos según dialecto
        aplicadas["mapeoTipos"] = dialecto switch
        {
            "postgresql" => new Dictionary<string, string>
            {
                ["int"] = "INTEGER",
                ["string"] = "VARCHAR(255)",
                ["boolean"] = "BOOLEAN",
                ["date"] = "DATE",
                ["datetime"] = "TIMESTAMP",
                ["decimal"] = "DECIMAL(10,2)",
                ["text"] = "TEXT"
            },
            "sqlserver" => new Dictionary<string, string>
            {
                ["int"] = "INT",
                ["string"] = "NVARCHAR(255)",
                ["boolean"] = "BIT",
                ["date"] = "DATE",
                ["datetime"] = "DATETIME2",
                ["decimal"] = "DECIMAL(10,2)",
                ["text"] = "NVARCHAR(MAX)"
            },
            // MySQL por defecto
            _ => new Dictionary<string, string>
            {
                ["int"] = "INT",
                ["string"] = "VARCHAR(255)",
                ["boolean"] = "TINYINT(1)",
                ["date"] = "DATE",
                ["datetime"] = "DATETIME",
                ["decimal"] = "DECIMAL(10,2)",
                ["text"] = "TEXT"
            }
        };
    }

    /// <summary>
    /// Aplica configuraciones específicas para generación MVC.
    /// </summary>
    private static void AplicarConfiguracionMVC(
        Dictionary<string, object> opciones,
        Dictionary<string, object> outputOptions,
        Dictionary<string, object> aplicadas,
        ContextoGeneracion contexto)
    {
        var framework = ObtenerString(opciones, "framework", "spring").ToLowerInvariant();
        var gestorDependencias = ObtenerString(opciones, "gestorDependencias", "maven").ToLowerInvariant();
        var paqueteBase = ObtenerString(opciones, "paqueteBase", "com.organizacion.proyecto");

        aplicadas["framework"] = framework;
        aplicadas["gestorDependencias"] = gestorDependencias;
        aplicadas["paqueteBase"] = paqueteBase;
        aplicadas["rutaPaquete"] = paqueteBase.Replace('.', '/');
        aplicadas["incluirComentarios"] = ObtenerBool(outputOptions, "incluirComentarios", true);
    }

    // ──────────────────────────────────────────
    // Helpers para extraer valores del diccionario
    // ──────────────────────────────────────────

    private static bool ObtenerBool(Dictionary<string, object> dict, string clave, bool defecto)
    {
        if (dict.TryGetValue(clave, out var valor))
        {
            return valor switch
            {
                bool b => b,
                string s => bool.TryParse(s, out var resultado) ? resultado : defecto,
                _ => defecto
            };
        }
        return defecto;
    }

    private static string ObtenerString(Dictionary<string, object> dict, string clave, string defecto)
    {
        if (dict.TryGetValue(clave, out var valor))
        {
            return valor?.ToString() ?? defecto;
        }
        return defecto;
    }
}
