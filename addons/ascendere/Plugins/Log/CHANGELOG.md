# Changelog

All notable changes to the Log Module will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-01-07

### Added
- `EnableLogLevel` property to toggle log level display in output
- `ILogService` interface for service integration patterns
- `LogService` class for dependency injection and service locator patterns
- Runtime override methods: `SetLoggingOverride()` and `RemoveLoggingOverride()`
- Extension methods for runtime overrides
- Comprehensive examples for service patterns

### Changed
- Logger no longer stored as serializable field in plugin (fixes ObjectDisposedException on recompile)
- Updated FormatMessage to conditionally include log level based on EnableLogLevel setting

### Fixed
- ObjectDisposedException during assembly reloads
- Potential memory leaks from improper Logger lifecycle management

## [1.0.0] - 2026-01-07

### Added
- Initial release of Log Module
- `[Log(true/false)]` attribute for class-level logging control
- Logger singleton with autoload support
- Four log levels: Debug, Info, Warning, Error
- Extension methods for convenient logging
- Reflection caching for performance
- Configurable output formatting (timestamps, class names)
- Global minimum log level filtering
- ILogger interface for dependency injection
- Comprehensive README with examples
