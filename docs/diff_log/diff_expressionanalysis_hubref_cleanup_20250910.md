# ExpressionAnalysis hub reference cleanup

- Removed automatic addition of `sync/1mLive`, `sync/1mFinal`, and `prev/1mFinal`.
- Dropped timeframe loops for `input/*Live`, `input/*Final`, and `prev/*Final`; only first window now maps to hub.
- Updated windowed query builder tests to avoid fixed `1m` metadata keys.
