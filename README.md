# Robotico.Resilience

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![GitHub Packages](https://img.shields.io/badge/GitHub%20Packages-Robotico.Resilience-blue?logo=github)](https://github.com/robotico-dev/robotico-resilience-csharp/packages)
[![Build](https://github.com/robotico-dev/robotico-resilience-csharp/actions/workflows/publish.yml/badge.svg)](https://github.com/robotico-dev/robotico-resilience-csharp/actions/workflows/publish.yml)

Retry and circuit breaker for .NET 8 and .NET 10. Operations return **Robotico.Result** for explicit success/error handling. Depends only on Robotico.Result.

## Robotico dependencies

```mermaid
flowchart LR
  A[Robotico.Resilience] --> B[Robotico.Result]
```

## Features

- **Retry** — `Retry.ExecuteAsync(operation)` (single run) or `Retry.ExecuteAsync(operation, policy, cancellationToken)` with `RetryPolicy` (max attempts, optional delay). Retries only when the operation returns an error Result.
- **RetryPolicy** — `RetryPolicy.Create(maxAttempts, delayBetweenAttempts)`.
- **CircuitBreaker** — Tracks consecutive failures; opens after threshold, allows one trial after break duration. `CircuitBreakerOptions.Create(failureThreshold, breakDuration)`.

## Installation

Add the GitHub Packages NuGet source (once per machine) so both packages can be restored:

```bash
dotnet nuget add source "https://nuget.pkg.github.com/robotico-dev/index.json" --name github --username YOUR_GITHUB_USERNAME --password YOUR_GITHUB_PAT --store-password-in-clear-text
```

Then add the packages:

```bash
dotnet add package Robotico.Resilience
dotnet add package Robotico.Result
```

**Robotico.Result** is published at [GitHub Packages (robotico-dev/robotico-results)](https://github.com/robotico-dev/robotico-results/pkgs/nuget/Robotico.Result). **Robotico.Resilience** is published at [GitHub Packages (robotico-dev/robotico-resilience-csharp)](https://github.com/robotico-dev/robotico-resilience-csharp/pkgs/nuget/Robotico.Resilience).

## Quick start

```csharp
using Robotico.Resilience;
using VoidResult = Robotico.Result.Result;

// Single execution (no retry)
VoidResult r = await Retry.ExecuteAsync(() => myService.SaveAsync());

// With retry: up to 4 attempts, 100 ms between attempts
RetryPolicy policy = RetryPolicy.Create(maxAttempts: 4, delayBetweenAttempts: TimeSpan.FromMilliseconds(100));
VoidResult r2 = await Retry.ExecuteAsync(() => myService.SaveAsync(), policy);

// Circuit breaker: open after 5 failures, break for 30 s
CircuitBreakerOptions options = CircuitBreakerOptions.Create(failureThreshold: 5, breakDuration: TimeSpan.FromSeconds(30));
CircuitBreaker cb = new CircuitBreaker(options);
VoidResult r3 = await cb.ExecuteAsync(() => myService.SaveAsync());
```

## Configuration (recommended)

**Use the factory methods for valid configuration.** Object initializers allow invalid values (e.g. `FailureThreshold = 0`); the constructors will throw. Prefer:

- `RetryPolicy.Create(maxAttempts, delayBetweenAttempts?, onRetry?)` — validates `maxAttempts >= 1`.
- `CircuitBreakerOptions.Create(failureThreshold, breakDuration?, onCircuitStateChange?, clock?)` — validates `failureThreshold >= 1`. Optional `clock` for testing (inject a manual clock to avoid `Task.Delay` in tests).

## Thread-safety

- **CircuitBreaker** — Thread-safe for typical single-instance usage: one `CircuitBreaker` per logical circuit (e.g. per downstream service). State is guarded with a lock; concurrent calls to `ExecuteAsync` are serialized for state updates. Do not share one instance across unrelated operations.
- **Retry** — Stateless; safe to call from multiple threads.

## Circuit open error contract

When the circuit is **Open**, `ExecuteAsync` does not call the operation and returns a failed **Result** with an error that has:

- **Message**: `"Circuit breaker is open."`
- **Code**: `"CIRCUIT_OPEN"` (so callers can detect and handle open-circuit explicitly).
- **Severity**: `Error`.

The error type is `Robotico.Result.Errors.SimpleError`. Use `result.IsError(out IError? err)` and inspect `err.Code` for `"CIRCUIT_OPEN"` if you need to distinguish open-circuit from other failures.

## Testing (injectable time)

To avoid flaky tests that rely on `Task.Delay`, inject an **IClock** into `CircuitBreakerOptions`. In tests, use a manual clock that you advance without real time passing:

```csharp
// In test project: ManualClock (see Robotico.Resilience.Tests)
var clock = new ManualClock();
var options = CircuitBreakerOptions.Create(failureThreshold: 1, breakDuration: TimeSpan.FromSeconds(10), clock: clock);
var cb = new CircuitBreaker(options);
// ... trigger open ...
clock.Advance(TimeSpan.FromSeconds(15));  // no Task.Delay
var result = await cb.ExecuteAsync(() => Task.FromResult(Result.Success()));
```

## Documentation

Detailed contract and design are in the `docs/` folder (AsciiDoc):

- **Design** (`docs/design.adoc`) — Circuit breaker state machine, retry semantics, thread-safety, error contract, versioning

To build HTML from the AsciiDoc sources (e.g. with Asciidoctor):

```bash
asciidoctor docs/design.adoc -o docs/design.html
```

## Building and testing

From the repo root:

```bash
dotnet restore
dotnet build -c Release
dotnet test tests/Robotico.Resilience.Tests/Robotico.Resilience.Tests.csproj -c Release
```

To run tests with code coverage (Coverlet; optional gate: fail if line coverage &lt; 90%):

```bash
dotnet test tests/Robotico.Resilience.Tests/Robotico.Resilience.Tests.csproj -c Release --collect:"XPlat Code Coverage"
# With threshold (CI): dotnet test ... -c Release --collect:"XPlat Code Coverage" /p:CollectCoverage=true /p:Threshold=90 /p:ThresholdType=line
```

To run benchmarks (optional; from the repo root; requires building the benchmark project first):

```bash
dotnet build benchmarks/Robotico.Resilience.Benchmarks/Robotico.Resilience.Benchmarks.csproj -c Release
dotnet run -c Release -p benchmarks/Robotico.Resilience.Benchmarks/Robotico.Resilience.Benchmarks.csproj -- --filter "*"
```

If you see an "Ambiguous project name" error when building the benchmark project, build from the repository root (e.g. in a clone of [robotico-dev/robotico-resilience-csharp](https://github.com/robotico-dev/robotico-resilience-csharp)).

## Analyzer suppressions (NoWarn)

The library intentionally suppresses the following analyzers; rationale is documented here for principal-grade transparency:

| Code | Rationale |
|------|------------|
| **RS0026** | `Retry.ExecuteAsync` has multiple overloads with optional parameters; the analyzer's suggestion would reduce API usability. |

All other warnings are treated as errors (`TreatWarningsAsErrors`). See `Directory.Build.props` and `.csproj` for the full build bar.

**Public API:** This library uses `Microsoft.CodeAnalysis.PublicApiAnalyzers`. To populate `PublicAPI.Shipped.txt`, apply the code fix "Add to PublicAPI.Unshipped" (Fix All in project), then at release move entries to `PublicAPI.Shipped.txt`.

## Dependencies

- **Robotico.Result**: Success/error results ([GitHub Packages](https://github.com/robotico-dev/robotico-results/pkgs/nuget/Robotico.Result))

## Versioning

We follow [Semantic Versioning](https://semver.org/). Version **1.0.0** is the first stable release. No breaking changes in minor/patch versions.

## License

See repository license file.
