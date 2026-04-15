# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## [1.0.0] - 2026-04-14

### Added

- `IRequest<TResponse>` and `IRequest` (void) marker interfaces
- `IRequestHandler<TRequest, TResponse>` for handling requests
- `INotification` and `INotificationHandler<TNotification>` for multicast notifications
- `IPipelineBehavior<TRequest, TResponse>` for cross-cutting pipeline concerns
- `ISender`, `IPublisher`, `IMediator` dispatching interfaces
- `Unit` readonly record struct for void-like returns
- `ServiceCollectionExtensions.AddCoderyMediator()` with assembly scanning
- `MediatorOptions.AddOpenBehavior()` for registering open generic pipeline behaviors
- Cached generic wrapper pattern for zero per-call reflection overhead
- Multi-target support: `net10.0` + `netstandard2.0`
