# diff: ForEachAsync dummy handling update (2025-08-30)

## Summary
- Adjusted `ForEachAsync` behavior to support `CompositeKeyPocoTests.SendAndReceive_CompositeKeyPoco`.
- Only the headered overload of `ForEachAsync` now allows messages with header `is_dummy=true` to pass through to the handler.
- The common overload `ForEachAsync(Func<T, Task>)` keeps existing behavior: dummy messages are skipped.

## Details
- Updated `src/EventSet.cs`:
  - Implemented the non-header overload to iterate and skip dummy messages explicitly, preserving prior behavior.
  - Modified the headered overload (`Func<T, Dictionary<string,string>, MessageMeta, Task>`) to no longer skip when `is_dummy=true`, enabling consumers that need header access to receive the priming/dummy records.

## Rationale
- Priming messages (`is_dummy=true`) are useful for consumers that read headers. To support scenarios validated by `CompositeKeyPoco` tests, header-aware handlers must receive these messages.
- To avoid behavior changes for existing consumers, the non-header overload continues to skip dummy messages.

## Impact
- Existing code using `ForEachAsync(Func<T, Task>)` remains unchanged and continues skipping dummy messages.
- Code using headered overloads (`ForEachAsync(Func<T, Dictionary<string,string>, MessageMeta, Task>)` and obsolete header-only overload) will now receive dummy messages.

## Follow-ups
- If any documentation states that "consumers always skip dummy messages", clarify that the skipping applies to the common overload; headered overloads may receive dummy events.

