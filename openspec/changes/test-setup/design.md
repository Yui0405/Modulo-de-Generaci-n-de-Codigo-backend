# Design: test-setup

## Overview

Create xUnit test project for GeneracionApi with unit tests for GeneracionService, IntegracionService, and pipeline filters.

## Test Project Structure

```
GeneracionApi/
├── GeneracionApi.csproj
└── GeneracionApi.Tests/                     ← NEW test project
    ├── GeneracionApi.Tests.csproj
    ├── Services/
    │   ├── GeneracionServiceTests.cs
    │   └── IntegracionServiceTests.cs
    └── Pipeline/
        └── Filters/
            ├── ValidacionMetamodeloFilterTests.cs
            └── TransformacionFilterTests.cs
```

## Test Project Configuration

- **Framework**: xUnit v3 (net8.0)
- **Reference**: GeneracionApi.csproj (project reference, not package)
- **Additional packages**:
  - Moq (mocking)
  - Microsoft.Extensions.Logging.Abstractions (for ILogger mocks)

## Implementation Details

### GeneracionServiceTests.cs

Test class uses:
- Mock<T> for IRepositorio<T> interfaces
- Mock<T> for IPipeline
- Mock<ITrazabilidadService>
- Mock<ILogger<GeneracionService>>
- Fixture or factory method to create service instance

Test categories:
- Validation (throwing ArgumentNullException/ArgumentException)
- Success scenarios with valid requests
- Retrieval scenarios (ObtenerPorIdAsync, ObtenerArchivoAsync, DescargarZipAsync)
- Regeneration scenarios

### IntegracionServiceTests.cs

Test class uses:
- Mock<IRepositorio<Generacion>>
- Mock<ILogger<IntegracionService>>
- Factory for creating test Generacion entities

Test categories:
- Empty proyectoId
- State inference (sin_integracion, error, activo)

### Pipeline Filter Tests

Test class uses:
- ContextoGeneracion creation helper
- Mock Func<ContextoGeneracion, Task> for "next" delegate

## Test Patterns

### Repository Mock Setup Pattern

```csharp
mockRepo.Setup(r => r.InsertarAsync(It.IsAny<Generacion>()))
    .ReturnsAsync((Generacion g) => g);
```

### Async Test Pattern

```csharp
[Fact]
public async Task GenerarAsync_Throws_WhenRequestIsNull()
{
    // Arrange
    var service = CreateService();
    
    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => 
        service.GenerarAsync(null!));
}
```

### ContextoGeneracion Helper

```csharp
private static ContextoGeneracion CreateContexto(
    string tipoDiagrama, 
    string modeloJson)
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
```

## Boundary Conditions

1. Test only public interface methods — no internal implementation details
2. Use in-memory mocks — no actual MongoDB connections
3. Test behavior, not implementation — mock all dependencies
4. Cover both happy paths and error/edge cases from spec

## Acceptance Criteria

- [ ] Test project compiles and runs
- [ ] All 30+ test scenarios from spec are implemented
- [ ] Tests use Moq for mocking dependencies
- [ ] Tests follow xUnit v3 patterns
- [ ] Test execution passes without errors