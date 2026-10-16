# Flutter UI Architecture

## Contents

- Respect the host project
- Boundaries
- Build performance
- Testing

## Respect the host project

Follow existing routing, state management, dependency injection, generated-code, naming, and testing conventions unless a targeted change is necessary. Avoid introducing a new architecture for one screen.

## Boundaries

Keep page orchestration, domain state, and reusable presentation distinct. Extract a widget when it has a clear contract, repeats, owns meaningful local behavior, or makes the parent readable. Avoid both thousand-line screens and fragmentation into meaningless wrappers.

## Build performance

Use `const`, narrow listeners/selectors, stable keys, lazy lists, image sizing/caching, and isolated repaint boundaries where evidence warrants them. Do not optimize blindly; keep expensive parsing, I/O, and allocation out of build/layout/paint paths.

## Testing

Test domain behavior separately from visual composition. Use widget tests for interactions and semantics, golden tests for stable visuals, and integration tests for critical journeys. Keep fixtures deterministic.

