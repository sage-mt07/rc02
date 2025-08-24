# Codex Tasks

## Physical Test Flow
1. Run docker-compose in `physicalTests/`
2. Execute dotnet physical tests
3. Collect results in `reports/physical`
4. Shutdown docker-compose
5. On success, commit reports/physical
