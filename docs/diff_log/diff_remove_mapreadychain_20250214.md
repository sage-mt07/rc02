# diff_remove_mapreadychain_20250214

## Summary
- Remove MapReadyChain and related interfaces (IMapReadyChain, IRetryReadyChain)
- Simplify error-handling API to use EventSet extensions directly

## Details
- Deleted MapReadyChain, RetryReadyChain, ErrorHandlingChain and associated interfaces
- Removed StartErrorHandling DSL; use `OnError` and `WithRetry` directly on `EventSet`
- Updated example and API docs accordingly

## Impact
- Mapping and retry chaining DSL is no longer available
- Error handling now relies on existing `EventSet.OnError`, `EventSet.WithRetry`, and `EventSet.Map`
