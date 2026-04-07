using System.Collections.Concurrent;

namespace GeneracionApi.Services.Pipeline;

/// <summary>
/// Implementación del orquestador del pipeline de generación.
/// 
/// Este es el "director de orquesta" del sistema. Su trabajo es coordinar
/// la ejecución de los filtros en el orden correcto.
/// 
/// NO conoce los filtros concretos — los recibe por inyección de dependencias
/// como IEnumerable&lt;IFiltroGeneracion&gt;. Esto permite agregar, quitar
/// o reordernar filtros sin modificar este código.
/// 
/// Flujo:
/// 1. Obtiene todos los filtros registrados
/// 2. Los ordena por la propiedad Orden (ascendente)
/// 3. Ejecuta cada filtro en secuencia
/// 4. Cada filtro recibe el contexto y llama al siguiente
/// 5. Si algún filtro falla, lanza GeneracionException
/// 
/// Analogía: es como un manager de línea de producción.
/// No opera las máquinas, pero asegura que operen en orden y paso a paso.
/// </summary>
public class GeneracionPipeline : IPipeline
{
    /// <summary>
    /// Lista de filtros inyectados por DI.
    /// El orden de ejecución se determina por la propiedad Orden de cada filtro.
    /// </summary>
    private readonly IEnumerable<IFiltroGeneracion> _filtros;

    public GeneracionPipeline(IEnumerable<IFiltroGeneracion> filtros)
    {
        _filtros = filtros ?? throw new ArgumentNullException(nameof(filtros));
    }

    /// <summary>
    /// Ejecuta todos los filtros del pipeline en orden secuencial.
    /// </summary>
    /// <param name="contexto">Contexto que fluye por todos los filtros.</param>
    /// <returns>El mismo contexto con los resultados de la ejecución.</returns>
    /// <exception cref="GeneracionException">Si algún filtro falla.</exception>
    public async Task<ContextoGeneracion> EjecutarAsync(ContextoGeneracion contexto)
    {
        // ──────────────────────────────────────────
        // Paso 1: Ordenar filtros por su propiedad Orden
        // Esto asegura que se ejecuten en el orden correcto:
        // 10 (Validación) → 20 (Transformación) → 30 (Config) → 40 (Generación) → 50 (Auditoría)
        // ──────────────────────────────────────────
        var filtrosOrdenados = _filtros
            .OrderBy(f => f.Orden)
            .ToList();

        // Verificar que hay filtros registrados
        if (filtrosOrdenados.Count == 0)
        {
            throw new GeneracionException(
                "No hay filtros registrados en el pipeline. Verificar la configuración.",
                new List<ValidationError>
                {
                    new("pipeline", "No se encontraron filtros para ejecutar", "Pipeline")
                });
        }

        // ──────────────────────────────────────────
        // Paso 2: Crear la cadena de ejecución
        // Construimos un delegate que representa "el resto de la cadena"
        // Desde el último filtro hasta el final (que no hace nada)
        // ──────────────────────────────────────────
        Task FinDePipeline(ContextoGeneracion ctx)
        {
            // El último filtro llama a esta función que no hace nada
            // Es como el final de la línea de producción
            return Task.CompletedTask;
        }

        // Vamos encadenando: primero el último filtro, luego el anterior...
        // hasta llegar al primero
        var siguiente = FinDePipeline;

        // Recorremos los filtros en orden INVERSO (de mayor a menor)
        // para construir la cadena: filter1 → filter2 → filter3 → ... → fin
        for (int i = filtrosOrdenados.Count - 1; i >= 0; i--)
        {
            var filtroActual = filtrosOrdenados[i];
            var siguienteActual = siguiente;

            // Creamos el siguiente eslabón de la cadena
            // El filtro actual hace su trabajo y luego llama a siguienteActual
            async Task<ContextoGeneracion> Closure(ContextoGeneracion ctx)
            {
                try
                {
                    // Registrar inicio del filtro (para debugging)
                    // Console.WriteLine($"[Pipeline] Ejecutando filtro: {filtroActual.Nombre} (Orden: {filtroActual.Orden})");

                    // Ejecutar el filtro actual
                    await filtroActual.EjecutarAsync(ctx, siguienteActual);

                    // Si llegamos aquí, el filtro terminó exitosamente
                    return ctx;
                }
                catch (GeneracionException)
                {
                    // Si el filtro ya lanzó GeneracionException, propagarla
                    // No envolvemos porque ya tiene los errores específicos
                    throw;
                }
                catch (Exception ex)
                {
                    // Error inesperado en el filtro — envolvemos en GeneracionException
                    throw new GeneracionException(
                        $"Error en filtro '{filtroActual.Nombre}': {ex.Message}",
                        new List<ValidationError>
                        {
                            new(filtroActual.Nombre, ex.Message, filtroActual.Nombre)
                        },
                        ex);
                }
            }

            siguiente = Closure;
        }

        // ──────────────────────────────────────────
        // Paso 3: Ejecutar el primer filtro de la cadena
        // Esto dispara toda la ejecución en cascada
        // ──────────────────────────────────────────
        try
        {
            await siguiente(contexto);
        }
        catch (GeneracionException)
        {
            // La excepción ya tiene toda la información necesaria
            // Solo marcamos el contexto como fallido
            contexto.FechaFin = DateTime.UtcNow;
            throw;
        }

        return contexto;
    }
}