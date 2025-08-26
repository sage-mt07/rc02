# chart UT tests 2025-08-26
- add ChartChecklistTests covering tumbling windows across minute/hour/day/week/month with proper TimeFrame→Tumbling order and multi-granularity arrays.
- add QueryAdapter tests via DerivationPlanner verifying hb/prev generation, live/final separation, and roll-up naming as `bar_<tf>_*`.
