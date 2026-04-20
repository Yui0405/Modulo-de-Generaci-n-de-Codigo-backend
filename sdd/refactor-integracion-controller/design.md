# Design: refactor-integracion-controller

## Technical Approach

Two independent refactors addressing technical debt:

1. **Optimized Query**: Add `ObtenerUltimaAsync` to repository interface and implement efficient MongoDB query using Sort + Limit instead of loading all records and filtering in memory.
2. **Error Handling Consolidation**: Extract duplicate try-catch blocks into a reusable helper method in the controller.

## Architecture Decisions

### Decision: Repository Method Signature

**Choice**: Add generic `ObtenerUltimaAsync<T>` method to `IRepositorio<T>` that accepts filter and order-by expressions.
```csharp
Task<T?> ObtenerUltimaAsync<T>(
    Expression<Func<T, bool>> filtro,
    Expression<Func<T, object>> ordenPorDesc) where T : class;
```

**Alternatives considered**: 
- Create a specialized method only in `GeneracionRepository` (rejected - violates generic repo pattern)
- Add pagination method with limit (rejected - too generic, doesn't express intent clearly)

**Rationale**: The generic interface already has `BuscarAsync`, `ContarAsync`, etc. Adding `ObtenerUltimaAsync` follows the same pattern and maintains interface consistency.

### Decision: Error Helper Implementation

**Choice**: Create `EjecutarAsync<T>(Func<Task<T>> action)` method that returns `Task<IActionResult>`.
```csharp
private async Task<IActionResult> EjecutarAsync<T>(
    Func<Task<T>> action,
    Func<T, IActionResult> onSuccess)
```

**Alternatives considered**:
- Return `ActionResult<T>` directly (rejected - doesn't handle error responses uniformly)
- Use policy pattern with Polly (rejected - overkill for this simple error handling)

**Rationale**: The current endpoints return different types ( IActionResult, ActionResult<GeneracionResponseDto>, etc.). A helper that returns `IActionResult` preserves flexibility while eliminating duplication.

### Decision: Interface Backward Compatibility

**Choice**: Add method with default throw `NotImplementedException` in `IRepositorio<T>` interface default implementation, or require all implementations to implement it.

**Rationale**: Looking at existing code, all repositories implement IRepositorio fully. No other projects depend on this library, so no compatibility risk.

## Data Flow

### Issue #1: Optimized Query

```
Controller → IntegracionService → IRepositorio<Generacion>
                                        ↓
                              GeneracionRepository
                                        ↓
                              MongoDB (Sort + Limit = 1)
```

Before: `BuscarAsync` → Load ALL → LINQ `OrderByDescending().FirstOrDefault()`
After: `ObtenerUltimaAsync` → MongoDB `SortByDescending().Limit(1)` → Single document

### Issue #2: Error Helper

```
Endpoint Request
       ↓
  EjecutarAsync(action)
       ↓
  try { action() }
  catch (ArgumentNullException) → BadRequest
  catch (ArgumentException) → BadRequest  
  catch (GeneracionException) → ValidationProblem
  catch (Exception) → 500 InternalServerError
       ↓
   IActionResult
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `Repositories/IRepositorio.cs` | Modify | Add `ObtenerUltimaAsync` method signature |
| `Repositories/GeneracionRepository.cs` | Modify | Implement optimized MongoDB query with Sort + Limit |
| `Services/IntegracionService.cs` | Modify | Use new `ObtenerUltimaAsync` instead of `BuscarAsync` + LINQ |
| `Controllers/GeneracionController.cs` | Modify | Add `EjecutarAsync` helper method, refactor all endpoints |

## Interfaces / Contracts

### IRepositorio.cs - New Method

```csharp
/// <summary>
/// Obtiene la entidad ordenada descendentemente por el campo especificado.
/// </summary>
/// <typeparam name="T">Tipo de entidad.</typeparam>
/// <param name="filtro">Filtro de búsqueda.</param>
/// <param name="ordenPorDesc">Expresión de ordenamiento descendente.</param>
/// <returns>La primera entidad ordenada o null si no hay resultados.</returns>
Task<T?> ObtenerUltimaAsync<T>(
    Expression<Func<T, bool>> filtro,
    Expression<Func<T, object>> ordenPorDesc) where T : class;
```

### GeneracionController - New Helper

```csharp
/// <summary>
/// Ejecuta una acción con manejo de errores centralizado.
/// </summary>
/// <typeparam name="T">Tipo de resultado exitoso.</typeparam>
/// <param name="action">Acción a ejecutar.</param>
/// <param name="onSuccess">Función para transformar el resultado en IActionResult.</param>
/// <returns>IActionResult con el resultado o error apropiado.</returns>
private async Task<IActionResult> EjecutarAsync<T>(
    Func<Task<T>> action,
    Func<T, IActionResult> onSuccess)
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | IntegracionService using new repository method | Mock IRepositorio, verify ObtenerUltimaAsync is called |
| Unit | GeneracionController error helper | Test each catch block returns correct status code |
| Integration | Full endpoint behavior | Existing tests should pass (no behavioral change) |

## Migration / Rollout

No migration required. This is a pure refactor with no database changes or feature flags.

## Open Questions

- None - the approach is straightforward and follows existing patterns.