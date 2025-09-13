# Diff: chart.md — WindowStart required note

- Add one-line note: WindowStart is required in Select for WhenEmpty/Prev/Fill bucket alignment.
- Location: points section near Select bullet.

Rationale
- Fill/Prev generation depends on a bucket column derived from WindowStart().
- Making the requirement explicit reduces misconfiguration and runtime errors.
