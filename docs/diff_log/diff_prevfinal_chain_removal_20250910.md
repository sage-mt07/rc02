# Diff: prevFinal chain removal

- Removed prevFinalId/liveInput chain starting at 1m; all roles consume `baseId_1s_final_s` directly.
- Added Final1s and Final1sStream role definitions with DDL generation.
- Tests updated for new `_1s_final_s` topic naming.
