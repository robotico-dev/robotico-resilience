# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- CI job `trim-validate` — build library with `IsTrimmable` and `EnableTrimAnalyzer`; publish requires trim-validate to pass.

## [1.0.0] — Initial release

- `Retry.ExecuteAsync(Func<Task<Result>>, RetryPolicy)` — retry with configurable max attempts and backoff.
- `RetryPolicy` — max attempts, delay factory.
- `CircuitBreaker` and `CircuitBreakerOptions` — failure threshold, open duration, half-open attempts.
- Integration with Robotico.Result (operations return `Result`; no exceptions for business failure).

[Unreleased]: https://github.com/robotico-dev/robotico-resilience-csharp/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/robotico-dev/robotico-resilience-csharp/releases/tag/v1.0.0
