# Proposal: test-setup

## Intent

Establecer una infraestructura de pruebas unitarias para el proyecto GeneracionApi que permita validar el comportamiento de los servicios principales y filtros del pipeline, garantizando calidad de código y facilitando refactorizaciones seguras.

## Scope

### In Scope

- Crear proyecto de tests xUnit: `GeneracionApi.Tests`
- Agregar dependencias: Moq (mocking), FluentAssertions (assertions)
- Configurar Coverlet para coverage
- Tests unitarios para `GeneracionService` (casos críticos: validación, éxito, error, regeneración)
- Tests unitarios para `IntegracionService` (consulta de estado)
- Tests unitarios para filtros del pipeline: `RegistroAuditoriaFilter`, `ValidacionMetamodeloFilter`
- Seguir estrategia de testing definida en `.agents/skills/dotnet-testing`

### Out of Scope

- Tests de integración con MongoDB real
- Tests E2E con Playwright
- Tests de carga (JMeter)
- Pruebas de rendimiento con BenchmarkDotNet

## Capabilities

### New Capabilities

- `test-infraestructura`: Infraestructura de testing con xUnit, Moq, FluentAssertions
- `tests-generacion-service`: Suite de tests unitarios para GeneracionService
- `tests-integracion-service`: Suite de tests unitarios para IntegracionService
- `tests-pipeline-filters`: Suite de tests unitarios para filtros del pipeline

## Approach

1. **Scaffold del proyecto de tests**: Crear `GeneracionApi.Tests.csproj` como proyecto de tipo xUnit, agregar al Solution
2. **Configuración de dependencias**: Agregar `Moq`, `FluentAssertions`, `coverlet.collector`
3. **Estructura de tests**: Crear carpetas `Services/`, `Pipeline/`, `Fixtures/` siguiendo convenciones
4. **Tests de GeneracionService**:
   - Probar validación de request nulo/vacío
   - Probar SourceType inválido
   - Probar éxito de generación
   - Probar manejo de excepciones del pipeline
   - Probar RegenerarAsync
5. **Tests de IntegracionService**:
   - Probar proyecto sin integraciones
   - Probar proyecto con última generación en error
   - Probar proyecto activo
6. **Tests de filtros**:
   - Probar RegistroAuditoriaFilter con éxito/error
   - Probar ValidacionMetamodeloFilter

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `GeneracionApi.Tests/` | New | Nuevo proyecto de tests |
| `Backend.sln` | Modified | Agregar referencia a proyecto de tests |
| `Services/GeneracionService.cs` | Indirect | Sujeto a tests (no modificar código) |
| `Services/IntegracionService.cs` | Indirect | Sujeto a tests (no modificar código) |
| `Services/Pipeline/Filters/` | Indirect | Filtros sujetos a tests |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Tests frágilles por acoplamiento a implementación | Medium | Usar mocks de interfaces, no de implementaciones concretas |
| Coverage bajo por falta de tiempo | Medium | Priorizar GeneracionService (>80% coverage) |
| Tests rotos al modificar servicios | Low | Revisar que tests sean de comportamiento, no de implementación |

## Rollback Plan

1. Eliminar directorio `GeneracionApi.Tests/`
2. Eliminar referencia del proyecto en `Backend.sln`
3. Eliminar packages del `GeneracionApi.csproj` si se agregaron directamente

## Dependencies

- .NET 8 SDK instalado
- Proyecto principal `GeneracionApi` compila correctamente

## Success Criteria

- [ ] Proyecto `GeneracionApi.Tests` compila sin errores
- [ ] Tests de GeneracionService pasan (>15 casos de prueba)
- [ ] Tests de IntegracionService pasan (>5 casos de prueba)
- [ ] Tests de filtros del pipeline pasan (>5 casos de prueba)
- [ ] Coverage报告显示 GeneracionService tiene coverage >70%
- [ ] `dotnet test` ejecuta todos los tests exitosamente