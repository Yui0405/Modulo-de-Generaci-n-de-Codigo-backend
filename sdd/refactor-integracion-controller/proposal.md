# Proposal: refactor-integracion-controller

## Intent

Refactor two tightly-coupled issues in the GeneracionApi project: (1) optimize IntegracionService to avoid loading all generations into memory just to get the last one, and (2) extract duplicate error handling from GeneracionController into reusable helper methods. Both address technical debt that currently harms performance and maintainability.

## Scope

### In Scope
- Add `ObtenerUltimaAsync(string proyectoId)` method to IRepositorio<Generacion> interface
- Implement the method in GeneracionRepository using MongoDB sort/limit
- Update IntegracionService to use the new optimized method
- Add error handling helper method to GeneracionController
- Apply helper to all controller endpoints with identical try-catch blocks

### Out of Scope
- Changes to other repositories or services
- Adding new endpoints or changing API contracts
- Performance optimization beyond Issue #1

## Capabilities

### New Capabilities
- `generacion-ultima-by-proyecto`: Optimized query to get most recent generation directly from MongoDB without loading all records

### Modified Capabilities
- `generacion-integracion`: Now uses efficient single-query approach instead of load-all-then-filter
- `generacion-controller`: Consolidated error handling reduces duplication

## Approach

For Issue #1: Add a new method to IRepositorio<T> that accepts a filter expression and ordering, returning only the top result. In GeneracionRepository, use MongoDB's `.SortByDescending().Limit(1)` pipeline. Update IntegracionService to call this method instead of `BuscarAsync` + LINQ `OrderByDescending().FirstOrDefault()`.

For Issue #2: Create a private async method `EjecutarAsync<T>(Func<Task<T>> action)` that wraps the try-catch blocks. The method catches ArgumentNullException, ArgumentException, GeneracionException (when T is GeneracionResponseDto), and generic Exception, returning appropriate IActionResult responses. Replace all endpoint try-catch blocks with single-line calls.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Repositories/IRepositorio.cs` | Modified | Add `ObtenerUltimaAsync` to generic interface |
| `Repositories/GeneracionRepository.cs` | Modified | Implement optimized MongoDB query |
| `Services/IntegracionService.cs` | Modified | Use new repository method |
| `Controllers/GeneracionController.cs` | Modified | Extract error handling helper |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Breaking existing IRepositorio implementations in other projects | Low | Add optional method with default throw NotImplementedException |
| Helper method too generic loses specific error context | Low | Keep distinct catch blocks for GeneracionException with its ValidationError list |

## Rollback Plan

1. Revert changes to IRepositorio.cs and GeneracionRepository.cs to remove new method
2. Revert IntegracionService.cs to use old BuscarAsync approach
3. Revert GeneracionController.cs to restore inline try-catch blocks (each endpoint ~15 lines)
4. No database migration needed - purely code refactor

## Dependencies

- None - this is a self-contained refactor with no external dependencies

## Success Criteria

- [ ] IntegracionService no longer calls BuscarAsync to load all generations for a proyecto
- [ ] New repository method returns single Generacion or null
- [ ] GeneracionController has 90%+ reduction in duplicate try-catch code
- [ ] All existing endpoints continue to return same HTTP status codes for same error types
- [ ] No functional changes to API behavior - only internal refactoring