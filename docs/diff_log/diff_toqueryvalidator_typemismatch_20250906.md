# ToQueryValidator type check

- Ensure `ValidateSelectMatchesPoco` compares property types between entity and select projection.
- Added unit test verifying mismatched types raise an exception.
- Decimal projection precision and scale are validated against entity attributes.
- Added unit test verifying decimal precision mismatch raises an exception.

