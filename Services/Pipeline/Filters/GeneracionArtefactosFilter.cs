using System.Text;

namespace GeneracionApi.Services.Pipeline.Filters;

/// <summary>
/// Filtro de generación de artefactos.
/// 
/// Es el CORAZÓN del sistema. Toma la estructura intermedia y las opciones
/// aplicadas, y genera los archivos de código fuente reales.
/// 
/// Los artefactos generados se guardan en contexto.Artefactos.
/// Cada artefacto es un ArtefactoGenerado con Ruta, Nombre, Contenido y Tipo.
/// 
/// Analogía: es la "fábrica". Los filtros anteriores prepararon los planos
/// y configuraron la máquina. Este filtro produce los productos.
/// </summary>
public class GeneracionArtefactosFilter : IFiltroGeneracion
{
    public string Nombre => "GeneracionArtefactos";

    public int Orden => 40;

    public Task EjecutarAsync(ContextoGeneracion contexto, Func<ContextoGeneracion, Task> siguiente)
    {
        var sourceType = contexto.Diagrama.TipoDiagrama.ToLowerInvariant();

        switch (sourceType)
        {
            case "classdiagram":
                GenerarJava(contexto);
                break;

            case "ermodel":
                GenerarSQL(contexto);
                break;

            case "mvcscaffold":
                GenerarScaffoldMVC(contexto);
                break;
        }

        // Actualizar contador en la generación
        contexto.FechaFin = DateTime.UtcNow;

        return siguiente(contexto);
    }

    // ══════════════════════════════════════════
    // GENERACIÓN JAVA (desde diagrama de clases)
    // ══════════════════════════════════════════

    private void GenerarJava(ContextoGeneracion contexto)
    {
        if (!contexto.Metadatos.TryGetValue("clasesProcesadas", out var clasesObj) ||
            clasesObj is not List<object> clases)
            return;

        var opciones = ObtenerOpciones(contexto);
        var paquete = opciones.GetValueOrDefault("paqueteBase", "com.organizacion.modelo")?.ToString()!;
        var incluirGetters = opciones.GetValueOrDefault("incluirGettersSetters", true) is true;
        var incluirConstructores = opciones.GetValueOrDefault("incluirConstructores", true) is true;
        var incluirComentarios = opciones.GetValueOrDefault("incluirComentarios", true) is true;
        var rutaPaquete = paquete.Replace('.', '/');

        foreach (dynamic clase in clases)
        {
            var codigo = GenerarClaseJava(
                clase,
                paquete,
                incluirGetters,
                incluirConstructores,
                incluirComentarios,
                contexto.TraceabilityOptions,
                contexto.GeneracionId,
                contexto.ProyectoId
            );

            contexto.Artefactos.Add(new Domain.ArtefactoGenerado
            {
                GeneracionId = contexto.GeneracionId,
                Ruta = $"src/main/java/{rutaPaquete}/{clase.NombreArchivo}",
                Nombre = clase.NombreArchivo,
                Contenido = codigo,
                Tipo = "java-class",
                TamanoBytes = Encoding.UTF8.GetByteCount(codigo),
                FechaCreacion = DateTime.UtcNow
            });
        }
    }

    private string GenerarClaseJava(
        dynamic clase,
        string paquete,
        bool incluirGetters,
        bool incluirConstructores,
        bool incluirComentarios,
        Dictionary<string, object> traceabilityOptions,
        string generacionId,
        string proyectoId)
    {
        var sb = new StringBuilder();

        // Package
        sb.AppendLine($"package {paquete};");
        sb.AppendLine();

        // Comentario de trazabilidad SOLO si el módulo externo aportó datos
        if (traceabilityOptions.Count > 0)
        {
            sb.AppendLine("/**");
            sb.AppendLine($" * Generado por A.G.I.L.E - Módulo de Generación");
            sb.AppendLine($" * Generación ID: {generacionId}");
            if (!string.IsNullOrWhiteSpace(proyectoId))
                sb.AppendLine($" * Proyecto: {proyectoId}");
            if (traceabilityOptions.TryGetValue("requisitoId", out var reqId))
                sb.AppendLine($" * Requisito: {reqId}");
            if (traceabilityOptions.TryGetValue("proyectoNombre", out var proyNom))
                sb.AppendLine($" * Proyecto: {proyNom}");
            if (traceabilityOptions.TryGetValue("modulo", out var modulo))
                sb.AppendLine($" * Módulo: {modulo}");
            if (traceabilityOptions.TryGetValue("version", out var version))
                sb.AppendLine($" * Versión: {version}");
            sb.AppendLine($" * Fecha: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine(" */");
        }

        // Comentario de clase
        if (incluirComentarios)
        {
            sb.AppendLine("/**");
            sb.AppendLine($" * Clase {clase.Nombre}.");
            sb.AppendLine(" */");
        }

        // Declaración de clase
        var declaracion = $"public class {clase.Nombre}";
        if (clase.HeredaDe != null)
        {
            declaracion += $" extends {clase.HeredaDe}";
        }
        if (clase.Implementa is List<string> interfaces && interfaces.Count > 0)
        {
            declaracion += $" implements {string.Join(", ", interfaces)}";
        }
        sb.AppendLine(declaracion + " {");

        // Atributos
        if (clase.Atributos is List<object> atributos)
        {
            foreach (dynamic attr in atributos)
            {
                sb.AppendLine($"    private {attr.Tipo} {attr.Nombre};");
            }
            sb.AppendLine();
        }

        // Constructor vacío
        if (incluirConstructores)
        {
            sb.AppendLine($"    public {clase.Nombre}() {{");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Constructor con parámetros
            if (clase.Atributos is List<object> attrs && attrs.Count > 0)
            {
                var parametros = string.Join(", ", ((List<object>)attrs)
                    .Select(a => $"{((dynamic)a).Tipo} {((dynamic)a).Nombre}"));
                sb.AppendLine($"    public {clase.Nombre}({parametros}) {{");
                foreach (dynamic attr in attrs)
                {
                    sb.AppendLine($"        this.{attr.Nombre} = {attr.Nombre};");
                }
                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        // Getters y Setters
        if (incluirGetters && clase.Atributos is List<object> atrs)
        {
            foreach (dynamic attr in atrs)
            {
                // Getter
                var nombreGetter = $"get{MayusculaInicial(attr.Nombre)}";
                sb.AppendLine($"    public {attr.Tipo} {nombreGetter}() {{");
                sb.AppendLine($"        return {attr.Nombre};");
                sb.AppendLine("    }");
                sb.AppendLine();

                // Setter
                var nombreSetter = $"set{MayusculaInicial(attr.Nombre)}";
                sb.AppendLine($"    public void {nombreSetter}({attr.Tipo} {attr.Nombre}) {{");
                sb.AppendLine($"        this.{attr.Nombre} = {attr.Nombre};");
                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        // Métodos personalizados
        if (clase.Metodos is List<object> metodos)
        {
            foreach (dynamic metodo in metodos)
            {
                if (incluirComentarios)
                {
                    sb.AppendLine("    /**");
                    sb.AppendLine($"     * {metodo.Nombre}.");
                    sb.AppendLine("     */");
                }
                sb.AppendLine($"    public {metodo.TipoRetorno} {metodo.Nombre}() {{");
                if (metodo.TipoRetorno != "void")
                {
                    sb.AppendLine("        // TODO: implementar");
                    sb.AppendLine($"        return null;");
                }
                else
                {
                    sb.AppendLine("        // TODO: implementar");
                }
                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ══════════════════════════════════════════
    // GENERACIÓN SQL (desde modelo E-R)
    // ══════════════════════════════════════════

    private void GenerarSQL(ContextoGeneracion contexto)
    {
        if (!contexto.Metadatos.TryGetValue("entidadesProcesadas", out var entidadesObj) ||
            entidadesObj is not List<object> entidades)
            return;

        var opciones = ObtenerOpciones(contexto);
        var dialecto = opciones.GetValueOrDefault("dialecto", "mysql")?.ToString()!;
        var incluirDrop = opciones.GetValueOrDefault("incluirDropIfExists", false) is true;
        var incluirComentarios = opciones.GetValueOrDefault("incluirComentarios", true) is true;

        var sb = new StringBuilder();

        // Header del script — SOLO si el módulo externo de trazabilidad aportó datos
        if (contexto.TraceabilityOptions.Count > 0)
        {
            sb.AppendLine($"-- Generado por A.G.I.L.E - Módulo de Generación");
            sb.AppendLine($"-- Generación ID: {contexto.GeneracionId}");
            if (!string.IsNullOrWhiteSpace(contexto.ProyectoId))
                sb.AppendLine($"-- Proyecto: {contexto.ProyectoId}");
            if (contexto.TraceabilityOptions.TryGetValue("requisitoId", out var reqId))
                sb.AppendLine($"-- Requisito: {reqId}");
            if (contexto.TraceabilityOptions.TryGetValue("proyectoNombre", out var proyNom))
                sb.AppendLine($"-- Proyecto: {proyNom}");
            if (contexto.TraceabilityOptions.TryGetValue("modulo", out var modulo))
                sb.AppendLine($"-- Módulo: {modulo}");
            sb.AppendLine($"-- Fecha: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"-- Dialecto: {dialecto.ToUpperInvariant()}");
            sb.AppendLine();
        }

        // Generar CREATE TABLE para cada entidad
        foreach (dynamic entidad in entidades)
        {
            if (incluirComentarios)
            {
                sb.AppendLine($"-- ════════════════════════════════════════");
                sb.AppendLine($"-- Tabla: {entidad.NombreTabla}");
                sb.AppendLine($"-- ════════════════════════════════════════");
            }

            if (incluirDrop)
            {
                sb.AppendLine($"DROP TABLE IF EXISTS {entidad.NombreTabla};");
                sb.AppendLine();
            }

            sb.AppendLine($"CREATE TABLE {entidad.NombreTabla} (");

            if (entidad.Columnas is List<object> columnas)
            {
                var definiciones = new List<string>();

                foreach (dynamic col in columnas)
                {
                    var def = $"    {col.Nombre} {col.TipoSql}";

                    if (col.EsPrimaryKey) def += " PRIMARY KEY";
                    if (col.NotNull && !col.EsPrimaryKey) def += " NOT NULL";
                    if (col.Unique && !col.EsPrimaryKey) def += " UNIQUE";

                    definiciones.Add(def);
                }

                sb.AppendLine(string.Join(",\n", definiciones));
            }

            sb.AppendLine(");");
            sb.AppendLine();
        }

        // Generar ALTER TABLE para foreign keys
        if (contexto.Metadatos.TryGetValue("relacionesProcesadas", out var relsObj) &&
            relsObj is List<object> relaciones && relaciones.Count > 0)
        {
            if (incluirComentarios)
            {
                sb.AppendLine($"-- ════════════════════════════════════════");
                sb.AppendLine($"-- Foreign Keys");
                sb.AppendLine($"-- ════════════════════════════════════════");
            }

            foreach (dynamic rel in relaciones)
            {
                var tablaOrigen = NombreATabla(rel.Origen?.ToString() ?? "");
                var tablaDestino = NombreATabla(rel.Destino?.ToString() ?? "");
                sb.AppendLine($"ALTER TABLE {tablaOrigen}");
                sb.AppendLine($"    ADD FOREIGN KEY ({tablaDestino}_id)");
                sb.AppendLine($"    REFERENCES {tablaDestino}(id);");
                sb.AppendLine();
            }
        }

        var contenido = sb.ToString();

        contexto.Artefactos.Add(new Domain.ArtefactoGenerado
        {
            GeneracionId = contexto.GeneracionId,
            Ruta = "schema/create_tables.sql",
            Nombre = "create_tables.sql",
            Contenido = contenido,
            Tipo = "sql-script",
            TamanoBytes = Encoding.UTF8.GetByteCount(contenido),
            FechaCreacion = DateTime.UtcNow
        });
    }

    // ══════════════════════════════════════════
    // GENERACIÓN SCAFFOLD MVC
    // ══════════════════════════════════════════

    private void GenerarScaffoldMVC(ContextoGeneracion contexto)
    {
        if (!contexto.Metadatos.TryGetValue("capasProcesadas", out var capasObj) ||
            capasObj is not List<object> capas)
            return;

        var opciones = ObtenerOpciones(contexto);
        var paqueteBase = opciones.GetValueOrDefault("paqueteBase", "com.organizacion.proyecto")?.ToString()!;
        var rutaPaquete = paqueteBase.Replace('.', '/');

        foreach (dynamic capa in capas)
        {
            if (capa.Clases is List<object> clases)
            {
                foreach (dynamic clase in clases)
                {
                    var codigo = GenerarClaseScaffold(
                        clase,
                        capa,
                        paqueteBase,
                        contexto.TraceabilityOptions,
                        contexto.GeneracionId,
                        contexto.ProyectoId
                    );

                    contexto.Artefactos.Add(new Domain.ArtefactoGenerado
                    {
                        GeneracionId = contexto.GeneracionId,
                        Ruta = $"src/main/java/{rutaPaquete}/{capa.Directorio}/{clase.Nombre}.java",
                        Nombre = $"{clase.Nombre}.java",
                        Contenido = codigo,
                        Tipo = "java-scaffold",
                        TamanoBytes = Encoding.UTF8.GetByteCount(codigo),
                        FechaCreacion = DateTime.UtcNow
                    });
                }
            }
        }
    }

    private string GenerarClaseScaffold(
        dynamic clase,
        dynamic capa,
        string paqueteBase,
        Dictionary<string, object> traceabilityOptions,
        string generacionId,
        string proyectoId)
    {
        var sb = new StringBuilder();
        var paqueteCompleto = $"{paqueteBase}.{capa.Directorio}";

        sb.AppendLine($"package {paqueteCompleto};");
        sb.AppendLine();

        // Comentario de trazabilidad SOLO si el módulo externo aportó datos
        if (traceabilityOptions.Count > 0)
        {
            sb.AppendLine("/**");
            sb.AppendLine($" * Generado por A.G.I.L.E - Módulo de Generación");
            sb.AppendLine($" * Generación ID: {generacionId}");
            if (!string.IsNullOrWhiteSpace(proyectoId))
                sb.AppendLine($" * Proyecto: {proyectoId}");
            sb.AppendLine($" * Capa: {capa.Nombre}");
            if (traceabilityOptions.TryGetValue("requisitoId", out var reqId))
                sb.AppendLine($" * Requisito: {reqId}");
            if (traceabilityOptions.TryGetValue("proyectoNombre", out var proyNom))
                sb.AppendLine($" * Proyecto: {proyNom}");
            if (traceabilityOptions.TryGetValue("modulo", out var modulo))
                sb.AppendLine($" * Módulo: {modulo}");
            sb.AppendLine($" * Fecha: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine(" */");
        }

        // Generar según el tipo
        var tipoClase = clase.Tipo?.ToString()?.ToLowerInvariant() ?? "class";

        switch (tipoClase)
        {
            case "interface":
                sb.AppendLine($"public interface {clase.Nombre} {{");
                sb.AppendLine("    // TODO: definir métodos");
                break;

            case "abstract":
                sb.AppendLine($"public abstract class {clase.Nombre} {{");
                sb.AppendLine("    // TODO: implementar métodos abstractos");
                break;

            default:
                sb.AppendLine($"public class {clase.Nombre} {{");
                sb.AppendLine("    // TODO: implementar");
                break;
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────

    private static Dictionary<string, object> ObtenerOpciones(ContextoGeneracion contexto)
    {
        if (contexto.Metadatos.TryGetValue("opcionesAplicadas", out var opcionesObj) &&
            opcionesObj is Dictionary<string, object> opciones)
        {
            return opciones;
        }
        return new Dictionary<string, object>();
    }

    private static string MayusculaInicial(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        return char.ToUpper(texto[0]) + texto[1..];
    }

    private static string NombreATabla(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return nombre;
        var resultado = new StringBuilder();
        for (int i = 0; i < nombre.Length; i++)
        {
            if (char.IsUpper(nombre[i]) && i > 0) resultado.Append('_');
            resultado.Append(char.ToLower(nombre[i]));
        }
        return resultado.ToString();
    }
}
