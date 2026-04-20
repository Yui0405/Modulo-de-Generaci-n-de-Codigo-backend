# Tasks: refactor-integracion-controller

## Phase 1: Repository Interface

- [ ] 1.1 Add `ObtenerUltimaAsync<T>` method to `Repositories/IRepositorio.cs` interface with filter and order-by expressions
- [ ] 1.2 Add XML documentation to the new method following existing pattern

## Phase 2: Repository Implementation

- [ ] 2.1 Implement `ObtenerUltimaAsync` in `Repositories/GeneracionRepository.cs` using MongoDB Sort + Limit(1)
- [ ] 2.2 Implement `ObtenerUltimaAsync` in other repositories (ConfigGeneracionRepository, DiagramaRepository, etc.) for interface completeness

## Phase 3: Service Update

- [ ] 3.1 Modify `Services/IntegracionService.cs` to use `ObtenerUltimaAsync` instead of `BuscarAsync` + LINQ filtering
- [ ] 3.2 Verify the service still returns the same response structure

## Phase 4: Controller Refactoring

- [ ] 4.1 Add `EjecutarAsync<T>` helper method to `Controllers/GeneracionController.cs`
- [ ] 4.2 Refactor `GenerarAsync` endpoint to use the helper
- [ ] 4.3 Refactor `ObtenerPorIdAsync` endpoint to use the helper
- [ ] 4.4 Refactor `ObtenerArchivoAsync` endpoint to use the helper
- [ ] 4.5 Refactor `DescargarZipAsync` endpoint to use the helper
- [ ] 4.6 Refactor `RegenerarAsync` endpoint to use the helper
- [ ] 4.7 Refactor `ConsultarIntegracionAsync` endpoint to use the helper
- [ ] 4.8 Verify all endpoints return same HTTP status codes as before

## Phase 5: Verification

- [ ] 5.1 Run existing tests to ensure no behavioral changes
- [ ] 5.2 Verify code compiles without errors
- [ ] 5.3 Verify controller has 90%+ reduction in duplicate try-catch code