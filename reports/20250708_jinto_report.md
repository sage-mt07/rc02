# KSQL Stream/Table Query Templates

JST 2025-07-08

All queries assume `VALUE_FORMAT='AVRO'` for serialization.

## Stream Queries

### DDL
```sql
CREATE STREAM orders (
  order_id INT KEY,
  customer_id INT,
  order_date STRING
) WITH (
  KAFKA_TOPIC='orders',
  VALUE_FORMAT='AVRO',
  PARTITIONS=1
);
```

### DML
```sql
INSERT INTO orders (order_id, customer_id, order_date)
VALUES (1, 101, '2025-07-08');
```

### Push Query
```sql
SELECT order_id, customer_id, order_date
FROM orders
EMIT CHANGES;
```

### Pull Query
`Pull` queries are not supported on streams. Use a table for point-in-time reads.

## Table Queries

### DDL
```sql
CREATE TABLE customers (
  customer_id INT PRIMARY KEY,
  name STRING,
  address STRING
) WITH (
  KAFKA_TOPIC='customers',
  VALUE_FORMAT='AVRO',
  PARTITIONS=1
);
```

### DML
```sql
INSERT INTO customers (customer_id, name, address)
VALUES (101, 'Alice', 'Tokyo');
```

### Push Query
```sql
SELECT customer_id, name
FROM customers
EMIT CHANGES;
```

### Pull Query
```sql
SELECT customer_id, name, address
FROM customers
WHERE customer_id = 101;
```

## Update Policy
- Examples conform to the official ksqlDB documentation.
- Revise these templates as new syntax or versions are released.

## Additional Query Variations

1. **Stream-Stream Join**
```sql
CREATE STREAM payments (
  payment_id INT KEY,
  order_id INT,
  amount DOUBLE
) WITH (
  KAFKA_TOPIC='payments',
  VALUE_FORMAT='AVRO'
);

SELECT o.order_id, p.amount
FROM orders o
JOIN payments p
  ON o.order_id = p.order_id
EMIT CHANGES;
```

2. **Stream-Table Join**
```sql
SELECT o.order_id, c.name
FROM orders o
LEFT JOIN customers c
  ON o.customer_id = c.customer_id
EMIT CHANGES;
```

3. **Table Join**
```sql
CREATE TABLE shipments (
  order_id INT PRIMARY KEY,
  status STRING
) WITH (
  KAFKA_TOPIC='shipments',
  VALUE_FORMAT='AVRO'
);

SELECT c.name, s.status
FROM customers c
JOIN shipments s
  ON c.customer_id = s.order_id;
```

4. **Stream Query with WHERE**
```sql
SELECT order_id, customer_id
FROM orders
WHERE customer_id = 101
EMIT CHANGES;
```

5. **Table Query with WHERE**
```sql
SELECT customer_id, name
FROM customers
WHERE address = 'Tokyo';
```

6. **Stream Aggregation with GROUP BY**
```sql
SELECT customer_id, COUNT(*) AS order_count
FROM orders
GROUP BY customer_id
EMIT CHANGES;
```

7. **Table Aggregation with GROUP BY**
```sql
SELECT address, COUNT(*) AS num_customers
FROM customers
GROUP BY address;
```

8. **Stream Join with GROUP BY**
```sql
SELECT c.customer_id, COUNT(o.order_id) AS order_total
FROM customers c
LEFT JOIN orders o
  ON c.customer_id = o.customer_id
GROUP BY c.customer_id
EMIT CHANGES;
```

9. **Table Join with GROUP BY**
```sql
SELECT c.address, COUNT(s.status) AS shipped_orders
FROM customers c
JOIN shipments s
  ON c.customer_id = s.order_id
GROUP BY c.address;
```

10. **Stream Join with Two Tables**
```sql
SELECT o.order_id, p.amount, c.name
FROM orders o
JOIN payments p ON o.order_id = p.order_id
JOIN customers c ON o.customer_id = c.customer_id
EMIT CHANGES;
```
