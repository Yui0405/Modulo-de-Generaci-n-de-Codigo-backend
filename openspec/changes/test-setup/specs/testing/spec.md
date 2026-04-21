# Testing Specification for test-setup

## Purpose

Establish unit testing capability for GeneracionApi services and pipeline filters using xUnit.

## ADDED Requirements

### Requirement: GeneracionService Unit Tests

The test suite MUST cover all business rules and public methods of GeneracionService.

#### Scenario: GenerarAsync throws when request is null

- GIVEN a null request
- WHEN GenerarAsync is called
- THEN ArgumentNullException is thrown with message "La solicitud de generación no puede ser nula."

#### Scenario: GenerarAsync throws when ProjectId is empty

- GIVEN a request with empty ProjectId
- WHEN GenerarAsync is called
- THEN ArgumentException is thrown with message "El ProjectId no puede estar vacío."

#### Scenario: GenerarAsync throws when SourceType is invalid

- GIVEN a request with SourceType "invalidType"
- WHEN GenerarAsync is called
- THEN ArgumentException is thrown mentioning valid values (classDiagram, erModel, mvcModel)

#### Scenario: GenerarAsync throws when ModelJson is empty

- GIVEN a request with empty ModelJson
- WHEN GenerarAsync is called
- THEN ArgumentException is thrown with message "El ModelJson no puede estar vacío."

#### Scenario: GenerarAsync succeeds with valid classDiagram request

- GIVEN a valid request with SourceType "classDiagram" and ModelJson containing {"clases":[{"nombre":"Usuario"}]}
- WHEN GenerarAsync is called
- THEN returns GeneracionResponseDto with Estado "exito" and at least one archivo

#### Scenario: GenerarAsync succeeds with valid erModel request

- GIVEN a valid request with SourceType "erModel" and ModelJson containing {"entidades":[{"nombre":"Usuario"}]}
- WHEN GenerarAsync is called
- THEN returns GeneracionResponseDto with Estado "exito"

#### Scenario: GenerarAsync succeeds with valid mvcModel request

- GIVEN a valid request with SourceType "mvcModel" and ModelJson containing {"capas":[{"nombre":"Controller"}]}
- WHEN GenerarAsync is called
- THEN returns GeneracionResponseDto with Estado "exito"

#### Scenario: ObtenerPorIdAsync returns null for empty ID

- GIVEN an empty generacionId
- WHEN ObtenerPorIdAsync is called
- THEN returns null

#### Scenario: ObtenerPorIdAsync returns null when not found

- GIVEN a non-existent generacionId
- WHEN ObtenerPorIdAsync is called
- THEN returns null

#### Scenario: ObtenerPorIdAsync returns response when found

- GIVEN an existing generacionId with artifacts
- WHEN ObtenerPorIdAsync is called
- THEN returns GeneracionResponseDto with GeneracionId and archivos

#### Scenario: ObtenerArchivoAsync returns null for empty params

- GIVEN empty generacionId or ruta
- WHEN ObtenerArchivoAsync is called
- THEN returns null

#### Scenario: ObtenerArchivoAsync returns null when not found

- GIVEN valid generacionId but non-existent ruta
- WHEN ObtenerArchivoAsync is called
- THEN returns null

#### Scenario: ObtenerArchivoAsync returns content when found

- GIVEN valid generacionId with matching ruta
- WHEN ObtenerArchivoAsync is called
- THEN returns the archivo contenido

#### Scenario: DescargarZipAsync returns null for empty ID

- GIVEN empty generacionId
- WHEN DescargarZipAsync is called
- THEN returns null

#### Scenario: DescargarZipAsync returns null when not found

- GIVEN a non-existent generacionId
- WHEN DescargarZipAsync is called
- THEN returns null

#### Scenario: DescargarZipAsync returns ZIP even with no artifacts

- GIVEN a valid generacionId but no artifacts
- WHEN DescargarZipAsync is called
- THEN returns valid ZIP bytes (not null)

#### Scenario: DescargarZipAsync returns ZIP with artifacts

- GIVEN a valid generacionId with artifacts
- WHEN DescargarZipAsync is called
- THEN returns ZIP bytes containing the artifact files

#### Scenario: RegenerarAsync returns null for empty ID

- GIVEN empty generacionId
- WHEN RegenerarAsync is called
- THEN returns null

#### Scenario: RegenerarAsync returns null when not found

- GIVEN a non-existent generacionId
- WHEN RegenerarAsync is called
- THEN returns null

#### Scenario: RegenerarAsync creates new generation with parent reference

- GIVEN an existing generacionId
- WHEN RegenerarAsync is called
- THEN returns new GeneracionResponseDto with GeneracionPadreId set to original ID

### Requirement: IntegracionService Unit Tests

The test suite MUST cover all business rules of IntegracionService.

#### Scenario: ConsultarAsync returns null for empty proyectoId

- GIVEN an empty proyectoId
- WHEN ConsultarAsync is called
- THEN returns null

#### Scenario: ConsultarAsync returns sin_integracion when count is zero

- GIVEN a proyectoId with no generations
- WHEN ConsultarAsync is called
- THEN returns object with EstadoIntegracion "sin_integracion"

#### Scenario: ConsultarAsync returns error when last generation has error

- GIVEN a proyectoId where last generation Estado is "error"
- WHEN ConsultarAsync is called
- THEN returns object with EstadoIntegracion "error"

#### Scenario: ConsultarAsync returns activo when at least one successful

- GIVEN a proyectoId where last generation Estado is "exito"
- WHEN ConsultarAsync is called
- THEN returns object with EstadoIntegracion "activo"

### Requirement: Pipeline Filters Unit Tests

The test suite MUST cover the validation and transformation filters.

#### Scenario: ValidacionMetamodeloFilter rejects empty sourceType

- GIVEN contexto with empty TipoDiagrama
- WHEN EjecutarAsync is called
- THEN adds validation error for sourceType

#### Scenario: ValidacionMetamodeloFilter rejects invalid sourceType

- GIVEN contexto with TipoDiagrama "invalidType"
- WHEN EjecutarAsync is called
- THEN adds validation error for sourceType

#### Scenario: ValidacionMetamodeloFilter rejects empty modelJson

- GIVEN contexto with empty ModeloJson
- WHEN EjecutarAsync is called
- THEN adds validation error for modelJson

#### Scenario: ValidacionMetamodeloFilter rejects invalid JSON

- GIVEN contexto with invalid JSON modelJson
- WHEN EjecutarAsync is called
- THEN adds validation error for modelJson

#### Scenario: ValidacionMetamodeloFilter accepts valid classDiagram

- GIVEN contexto with valid classDiagram {"clases":[{"nombre":"Usuario"}]}
- WHEN EjecutarAsync is called
- THEN passes validation (no errors)

#### Scenario: ValidacionMetamodeloFilter accepts valid erModel

- GIVEN contexto with valid erModel {"entidades":[{"nombre":"Usuario"}]}
- WHEN EjecutarAsync is called
- THEN passes validation (no errors)

#### Scenario: ValidacionMetamodeloFilter rejects classDiagram without classes array

- GIVEN contexto with classDiagram missing "clases" array
- WHEN EjecutarAsync is called
- THEN adds validation error for "clases"

#### Scenario: ValidacionMetamodeloFilter rejects classDiagram with empty classes

- GIVEN contexto with classDiagram {"clases":[]}
- WHEN EjecutarAsync is called
- THEN adds validation error for "clases"

#### Scenario: ValidacionMetamodeloFilter rejects classDiagram class without nombre

- GIVEN contexto with classDiagram {"clases":[{"atributos":[]}]}
- WHEN EjecutarAsync is called
- THEN adds validation error for clase nombre

#### Scenario: TransformacionFilter transforms classDiagram

- GIVEN contexto with classDiagram {"clases":[{"nombre":"Usuario","atributos":[{"nombre":"Id","tipo":"int"}]}]}
- WHEN EjecutarAsync is called
- THEN contexto.Metadatos contains "clasesProcesadas"

#### Scenario: TransformacionFilter transforms erModel

- GIVEN contexto with erModel {"entidades":[{"nombre":"Usuario","atributos":[{"nombre":"Id","tipo":"int"}]}]}
- WHEN EjecutarAsync is called
- THEN contexto.Metadatos contains "entidadesProcesadas"

#### Scenario: TransformacionFilter transforms mvcModel

- GIVEN contexto with mvcModel {"capas":[{"nombre":"Controller"}]}
- WHEN EjecutarAsync is called
- THEN contexto.Metadatos contains "capasProcesadas"