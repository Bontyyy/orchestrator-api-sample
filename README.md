# orchestrator-api-sample

Sample .NET 8 component repository used by the [Specfuse Orchestrator](https://github.com/clabonte/orchestrator) Phase 1 walkthrough. Not a real product.

## Purpose

This repository exists to exercise the component agent end-to-end on realistic tasks: picking up a GitHub issue conforming to the work unit contract, writing code, running the six mandatory verification gates, opening a pull request, and transitioning through the task state machine. Its code intentionally has the shape of a typical small API project (domain record + application service + controller + tests) so the walkthrough tasks resemble what a component agent will do in a real codebase.

## Structure

```
src/
  OrchestratorApiSample.Domain/         # Widget record and related primitives
  OrchestratorApiSample.Application/    # Widget service, repository port, validation exceptions
  OrchestratorApiSample.Api/            # ASP.NET Core controllers, DI wiring, in-memory persistence
tests/
  OrchestratorApiSample.Tests/          # xUnit + FluentAssertions + Moq
.specfuse/
  verification.yml                      # Contract consumed by the component agent verification skill
.github/workflows/
  ci.yml                                # Runs the six verification gates on every push and PR
```

## Running locally

```sh
dotnet build
dotnet test
dotnet format --verify-no-changes
```

## Relation to the orchestrator

The component agent in the orchestrator reads `.specfuse/verification.yml` at the repo root to discover which commands to run for each verification gate. GitHub branch protection on `main` consumes the same command set via the `ci.yml` workflow, so the agent's self-declared verification aligns with what CI enforces.

Changes are delivered through pull requests; direct pushes to `main` are disallowed by branch protection.
