# diff: key schema optional metadata (2025-09-03)
- omit KEY_AVRO_SCHEMA_FULL_NAME when key schema is not supplied
- still emit KEY_FORMAT='AVRO' and VALUE_AVRO_SCHEMA_FULL_NAME
