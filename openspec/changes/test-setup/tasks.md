# Tasks: test-setup

## Phase 1: Test Project Infrastructure

- [x] 1.1 Create `GeneracionApi.Tests/GeneracionApi.Tests.csproj` targeting net8.0
- [x] 1.2 Add project reference to GeneracionApi.csproj
- [x] 1.3 Add Moq package reference
- [x] 1.4 Add to Backend.sln solution

## Phase 2: GeneracionService Tests

- [x] 2.1 Create test class `GeneracionServiceTests.cs`
- [x] 2.2 Add validation tests (null request, empty ProjectId, invalid SourceType, empty ModelJson)
- [x] 2.3 Add success tests for classDiagram
- [x] 2.4 Add success tests for erModel
- [x] 2.5 Add success tests for mvcModel
- [x] 2.6 Add ObtenerPorIdAsync tests
- [x] 2.7 Add ObtenerArchivoAsync tests
- [x] 2.8 Add DescargarZipAsync tests
- [x] 2.9 Add RegenerarAsync tests

## Phase 3: IntegracionService Tests

- [x] 3.1 Create test class `IntegracionServiceTests.cs`
- [x] 3.2 Add ConsultarAsync null proyectoId test
- [x] 3.3 Add ConsultarAsync sin_integracion test
- [x] 3.4 Add ConsultarAsync error state test
- [x] 3.5 Add ConsultarAsync activo state test

## Phase 4: Pipeline Filter Tests

- [x] 4.1 Create test class `ValidacionMetamodeloFilterTests.cs`
- [x] 4.2 Add empty/invalid sourceType tests
- [x] 4.3 Add empty/invalid modelJson tests
- [x] 4.4 Add valid classDiagram tests
- [x] 4.5 Add valid erModel tests
- [x] 4.6 Add classDiagram structure validation tests
- [x] 4.7 Create test class `TransformacionFilterTests.cs`
- [x] 4.8 Add classDiagram transform test
- [x] 4.9 Add erModel transform test
- [x] 4.10 Add mvcModel transform test

## Phase 5: Verification

- [ ] 5.1 Run dotnet test to verify compilation (blocked by network)
- [ ] 5.2 Run all tests and verify they pass (blocked by network)