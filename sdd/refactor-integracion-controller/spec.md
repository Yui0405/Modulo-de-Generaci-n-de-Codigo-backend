# Delta: IntegracionService + Controller

## ADDED Requirements

### Requirement: Optimized Last Generation Query

The system MUST provide an efficient method to retrieve the most recent generation for a given proyectoId directly from MongoDB without loading all matching records into memory.

#### Scenario: Retrieve last generation successfully

- GIVEN a proyectoId with at least one Generacion record in the database
- WHEN `ObtenerUltimaAsync` is called with a filter for that proyectoId
- THEN the Generacion with the most recent `FechaCreacion` is returned
- AND no other Generacion records are loaded from the database

#### Scenario: No generations for proyecto

- GIVEN a proyectoId with zero Generacion records in the database
- WHEN `ObtenerUltimaAsync` is called with a filter for that proyectoId
- THEN null is returned
- AND no exception is thrown

### Requirement: Error Handling Helper Method

The system MUST provide a reusable error handling method in the controller to reduce code duplication across endpoints while preserving specific error context.

#### Scenario: Handle ArgumentNullException

- GIVEN an endpoint calls the helper with an action that throws ArgumentNullException
- WHEN the action is executed
- THEN the helper returns BadRequest with the exception message
- AND logs the exception as a warning

#### Scenario: Handle ArgumentException

- GIVEN an endpoint calls the helper with an action that throws ArgumentException
- WHEN the action is executed
- THEN the helper returns BadRequest with the exception message
- AND logs the exception as a warning

#### Scenario: Handle GeneracionException

- GIVEN an endpoint calls the helper with an action that throws GeneracionException
- WHEN the action is executed
- THEN the helper returns 400 with ValidationProblem containing all validation errors
- AND logs the exception as a warning

#### Scenario: Handle unexpected exception

- GIVEN an endpoint calls the helper with an action that throws an unexpected exception
- WHEN the action is executed
- THEN the helper returns 500 InternalServerError with the exception message
- AND logs the exception as an error

#### Scenario: Successful execution

- GIVEN an endpoint calls the helper with an action that completes successfully
- WHEN the action is executed
- THEN the helper returns the Ok result from the action
- AND no error is logged

## MODIFIED Requirements

### Requirement: ConsultarAsync Integration Status

The system MUST return the integration status for a proyecto using the most recent Generacion. The service MUST use an efficient single-query approach to retrieve the last generation rather than loading all generations and filtering in memory.

#### Scenario: Query integration with existing generations (unchanged)

- GIVEN a proyectoId with one or more Generacion records
- WHEN `ConsultarAsync` is called
- THEN it returns estadoIntegracion as "activo" if the last generation has no error, or "error" if the last generation has Estado == "error"
- AND returns the last generation details

#### Scenario: Query integration with no generations (unchanged)

- GIVEN a proyectoId with zero Generacion records
- WHEN `ConsultarAsync` is called
- THEN it returns estadoIntegracion as "sin_integracion"
- AND returns null for UltimaGeneracion

(Previously: Used BuscarAsync + LINQ OrderByDescending + FirstOrDefault)

### Requirement: Controller Endpoints Error Handling (unchanged behavior)

All controller endpoints MUST return the same HTTP status codes for the same error types as before the refactor. The helper method is an internal implementation detail that preserves the existing error response contracts.

#### Scenario: Endpoint returns 400 for invalid input

- GIVEN an endpoint receives invalid input (empty generacionId)
- WHEN the endpoint is called
- THEN it returns 400 BadRequest with error message

#### Scenario: Endpoint returns 404 for missing resource

- GIVEN an endpoint requests a non-existent resource
- WHEN the endpoint is called
- THEN it returns 404 NotFound with error message

#### Scenario: Endpoint returns 500 for unexpected errors

- GIVEN an endpoint encounters an unexpected error
- WHEN the endpoint is called
- THEN it returns 500 InternalServerError with error message