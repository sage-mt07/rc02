# Physical Test Report

- Datetime (UTC): 2025-08-29 07:41:18
- Result: Failed
- Category: infra

## Evidence

### compose_up.log (excerpt)

     1	NAME                            IMAGE                                   COMMAND                  SERVICE           CREATED       STATUS                        PORTS
     2	physicaltests-kafka-1           confluentinc/cp-kafka:7.4.3             "/etc/confluent/dock…"   kafka             13 days ago   Exited (1) 35 seconds ago     
     3	physicaltests-ksqldb-cli-1      confluentinc/ksqldb-cli:0.29.0          "/bin/sh"                ksqldb-cli        13 days ago   Exited (255) 15 minutes ago   
     4	physicaltests-ksqldb-server-1   confluentinc/ksqldb-server:0.29.0       "/usr/bin/docker/run"    ksqldb-server     13 days ago   Exited (255) 15 minutes ago   0.0.0.0:8088->8088/tcp
     5	physicaltests-zookeeper-1       confluentinc/cp-zookeeper:7.4.3         "/etc/confluent/dock…"   zookeeper         13 days ago   Up 45 seconds                 0.0.0.0:2181->2181/tcp, [::]:2181->2181/tcp
     6	schema-registry                 confluentinc/cp-schema-registry:7.4.3   "/etc/confluent/dock…"   schema-registry   13 days ago   Up 44 seconds                 0.0.0.0:8081->8081/tcp, [::]:8081->8081/tcp
     7	schema-registry  | ===> User
     8	schema-registry  | uid=1000(appuser) gid=1000(appuser) groups=1000(appuser)
     9	schema-registry  | ===> Configuring ...
    10	schema-registry  | ===> Running preflight checks ... 
    11	schema-registry  | ===> Check if Kafka is healthy ...
    12	schema-registry  | [2025-08-15 09:10:04,056] INFO AdminClientConfig values: 
    13	schema-registry  | 	auto.include.jmx.reporter = true
    14	schema-registry  | 	bootstrap.servers = [PLAINTEXT://kafka:29092]
    15	kafka-1          | ===> User
    16	kafka-1          | uid=1000(appuser) gid=1000(appuser) groups=1000(appuser)
    17	kafka-1          | ===> Configuring ...
    18	kafka-1          | Running in Zookeeper mode...
    19	kafka-1          | ===> Running preflight checks ... 
    20	kafka-1          | ===> Check if /var/lib/kafka/data is writable ...
    21	kafka-1          | ===> Check if Zookeeper is healthy ...
    22	kafka-1          | [2025-08-15 09:10:04,277] INFO Client environment:zookeeper.version=3.6.3--6401e4ad2087061bc6b9f80dec2d69f2e3c8660a, built on 04/08/2021 16:35 GMT (org.apache.zookeeper.ZooKeeper)
    23	kafka-1          | [2025-08-15 09:10:04,277] INFO Client environment:host.name=33931b49e875 (org.apache.zookeeper.ZooKeeper)
    24	kafka-1          | [2025-08-15 09:10:04,277] INFO Client environment:java.version=11.0.20 (org.apache.zookeeper.ZooKeeper)
    25	kafka-1          | [2025-08-15 09:10:04,277] INFO Client environment:java.vendor=Azul Systems, Inc. (org.apache.zookeeper.ZooKeeper)
    26	kafka-1          | [2025-08-15 09:10:04,277] INFO Client environment:java.home=/usr/lib/jvm/java-11-zulu-openjdk-ca (org.apache.zookeeper.ZooKeeper)
    27	ksqldb-server-1  | ===> Configuring ksqlDB...
    28	ksqldb-server-1  | ===> Launching ksqlDB Server...
    29	ksqldb-server-1  | [2025-08-15 09:10:11,947] INFO KsqlConfig values: 
    30	ksqldb-server-1  | 	ksql.access.validator.enable = auto
    31	ksqldb-server-1  | 	ksql.assert.schema.default.timeout.ms = 1000
    32	ksqldb-server-1  | 	ksql.assert.topic.default.timeout.ms = 1000
    33	ksqldb-server-1  | 	ksql.authorization.cache.expiry.time.secs = 30
    34	ksqldb-server-1  | 	ksql.authorization.cache.max.entries = 10000
    35	ksqldb-server-1  | 	ksql.cast.strings.preserve.nulls = true
    36	ksqldb-server-1  | 	ksql.connect.basic.auth.credentials.file = 
    37	ksqldb-server-1  | 	ksql.connect.basic.auth.credentials.reload = false
    38	ksqldb-server-1  | 	ksql.connect.basic.auth.credentials.source = NONE
    39	ksqldb-server-1  | 	ksql.connect.request.headers.plugin = null
    40	ksqldb-server-1  | 	ksql.connect.request.timeout.ms = 5000
    41	ksqldb-server-1  | 	ksql.connect.url = http://localhost:8083
    42	ksqldb-server-1  | 	ksql.connect.worker.config = 
    43	ksqldb-server-1  | 	ksql.create.or.replace.enabled = true
    44	ksqldb-server-1  | 	ksql.endpoint.migrate.query = true
    45	ksqldb-server-1  | 	ksql.error.classifier.regex = 
    46	ksqldb-server-1  | 	ksql.extension.dir = ext
    47	ksqldb-server-1  | 	ksql.headers.columns.enabled = true
    48	ksqldb-server-1  | 	ksql.hidden.topics = [_confluent.*, __confluent.*, _schemas, __consumer_offsets, __transaction_state, connect-configs, connect-offsets, connect-status, connect-statuses]
    49	ksqldb-server-1  | 	ksql.insert.into.values.enabled = true
    50	ksqldb-server-1  | 	ksql.internal.topic.min.insync.replicas = 1
    51	ksqldb-server-1  | 	ksql.internal.topic.replicas = 1
    52	ksqldb-server-1  | 	ksql.json_sr.converter.deserializer.enabled = true
    53	ksqldb-server-1  | 	ksql.lambdas.enabled = true
    54	ksqldb-server-1  | 	ksql.metastore.backup.location = 
    55	ksqldb-server-1  | 	ksql.metrics.extension = null
    56	ksqldb-server-1  | 	ksql.metrics.tags.custom = 
    57	ksqldb-server-1  | 	ksql.nested.error.set.null = true
    58	ksqldb-server-1  | 	ksql.new.query.planner.enabled = false
    59	ksqldb-server-1  | 	ksql.output.topic.name.prefix = 
    60	ksqldb-server-1  | 	ksql.persistence.default.format.key = KAFKA
    61	ksqldb-server-1  | 	ksql.persistence.default.format.value = null
    62	ksqldb-server-1  | 	ksql.persistence.wrap.single.values = null
    63	ksqldb-server-1  | 	ksql.persistent.prefix = query_
    64	ksqldb-server-1  | 	ksql.properties.overrides.denylist = []
    65	ksqldb-server-1  | 	ksql.pull.queries.enable = true
    66	ksqldb-server-1  | 	ksql.query.cleanup.shutdown.timeout.ms = 30000
    67	ksqldb-server-1  | 	ksql.query.error.max.queue.size = 10
    68	ksqldb-server-1  | 	ksql.query.persistent.active.limit = 2147483647
    69	ksqldb-server-1  | 	ksql.query.persistent.max.bytes.buffering.total = -1
    70	ksqldb-server-1  | 	ksql.query.pull.enable.standby.reads = false
    71	ksqldb-server-1  | 	ksql.query.pull.forwarding.timeout.ms = 20000
    72	ksqldb-server-1  | 	ksql.query.pull.interpreter.enabled = true
    73	ksqldb-server-1  | 	ksql.query.pull.limit.clause.enabled = true
    74	ksqldb-server-1  | 	ksql.query.pull.max.allowed.offset.lag = 9223372036854775807
    75	ksqldb-server-1  | 	ksql.query.pull.max.concurrent.requests = 2147483647
    76	ksqldb-server-1  | 	ksql.query.pull.max.hourly.bandwidth.megabytes = 2147483647
    77	ksqldb-server-1  | 	ksql.query.pull.max.qps = 2147483647
    78	ksqldb-server-1  | 	ksql.query.pull.metrics.enabled = true
    79	ksqldb-server-1  | 	ksql.query.pull.range.scan.enabled = true
    80	ksqldb-server-1  | 	ksql.query.pull.router.thread.pool.size = 50
    81	ksqldb-server-1  | 	ksql.query.pull.stream.enabled = true
    82	schema-registry  | 	client.dns.lookup = use_all_dns_ips
    83	schema-registry  | 	client.id = 
    84	schema-registry  | 	connections.max.idle.ms = 300000
    85	schema-registry  | 	default.api.timeout.ms = 60000
    86	schema-registry  | 	metadata.max.age.ms = 300000
    87	schema-registry  | 	metric.reporters = []
    88	schema-registry  | 	metrics.num.samples = 2
    89	schema-registry  | 	metrics.recording.level = INFO
    90	schema-registry  | 	metrics.sample.window.ms = 30000
    91	schema-registry  | 	receive.buffer.bytes = 65536
    92	schema-registry  | 	reconnect.backoff.max.ms = 1000
    93	schema-registry  | 	reconnect.backoff.ms = 50
    94	schema-registry  | 	request.timeout.ms = 30000
    95	schema-registry  | 	retries = 2147483647
    96	schema-registry  | 	retry.backoff.ms = 100
    97	schema-registry  | 	sasl.client.callback.handler.class = null
    98	schema-registry  | 	sasl.jaas.config = null
    99	schema-registry  | 	sasl.kerberos.kinit.cmd = /usr/bin/kinit
   100	schema-registry  | 	sasl.kerberos.min.time.before.relogin = 60000
   101	schema-registry  | 	sasl.kerberos.service.name = null
   102	schema-registry  | 	sasl.kerberos.ticket.renew.jitter = 0.05
   103	schema-registry  | 	sasl.kerberos.ticket.renew.window.factor = 0.8
   104	schema-registry  | 	sasl.login.callback.handler.class = null
   105	schema-registry  | 	sasl.login.class = null
   106	schema-registry  | 	sasl.login.connect.timeout.ms = null
   107	schema-registry  | 	sasl.login.read.timeout.ms = null
   108	schema-registry  | 	sasl.login.refresh.buffer.seconds = 300
   109	schema-registry  | 	sasl.login.refresh.min.period.seconds = 60
   110	schema-registry  | 	sasl.login.refresh.window.factor = 0.8
   111	schema-registry  | 	sasl.login.refresh.window.jitter = 0.05
   112	schema-registry  | 	sasl.login.retry.backoff.max.ms = 10000
   113	schema-registry  | 	sasl.login.retry.backoff.ms = 100
   114	schema-registry  | 	sasl.mechanism = GSSAPI
   115	schema-registry  | 	sasl.oauthbearer.clock.skew.seconds = 30
   116	schema-registry  | 	sasl.oauthbearer.expected.audience = null
   117	schema-registry  | 	sasl.oauthbearer.expected.issuer = null
   118	schema-registry  | 	sasl.oauthbearer.jwks.endpoint.refresh.ms = 3600000
   119	kafka-1          | [2025-08-15 09:10:04,277] INFO Client environment:java.class.path=/usr/share/java/cp-base-new/scala-java8-compat_2.13-1.0.2.jar:/usr/share/java/cp-base-new/utility-belt-7.4.3.jar:/usr/share/java/cp-base-new/kafka-storage-7.4.3-ccs.jar:/usr/share/java/cp-base-new/scala-collection-compat_2.13-2.10.0.jar:/usr/share/java/cp-base-new/zookeeper-3.6.3.jar:/usr/share/java/cp-base-new/jackson-module-scala_2.13-2.14.2.jar:/usr/share/java/cp-base-new/scala-logging_2.13-3.9.4.jar:/usr/share/java/cp-base-new/disk-usage-agent-7.4.3.jar:/usr/share/java/cp-base-new/argparse4j-0.7.0.jar:/usr/share/java/cp-base-new/jmx_prometheus_javaagent-0.18.0.jar:/usr/share/java/cp-base-new/jackson-databind-2.14.2.jar:/usr/share/java/cp-base-new/paranamer-2.8.jar:/usr/share/java/cp-base-new/scala-reflect-2.13.10.jar:/usr/share/java/cp-base-new/common-utils-7.4.3.jar:/usr/share/java/cp-base-new/metrics-core-4.1.12.1.jar:/usr/share/java/cp-base-new/minimal-json-0.9.5.jar:/usr/share/java/cp-base-new/audience-annotations-0.5.0.jar:/usr/share/java/cp-base-new/kafka-clients-7.4.3-ccs.jar:/usr/share/java/cp-base-new/snappy-java-1.1.10.5.jar:/usr/share/java/cp-base-new/jackson-core-2.14.2.jar:/usr/share/java/cp-base-new/commons-cli-1.4.jar:/usr/share/java/cp-base-new/jolokia-jvm-1.7.1.jar:/usr/share/java/cp-base-new/kafka-metadata-7.4.3-ccs.jar:/usr/share/java/cp-base-new/slf4j-api-1.7.36.jar:/usr/share/java/cp-base-new/kafka_2.13-7.4.3-ccs.jar:/usr/share/java/cp-base-new/scala-library-2.13.10.jar:/usr/share/java/cp-base-new/jolokia-core-1.7.1.jar:/usr/share/java/cp-base-new/kafka-group-coordinator-7.4.3-ccs.jar:/usr/share/java/cp-base-new/jose4j-0.9.3.jar:/usr/share/java/cp-base-new/jackson-datatype-jdk8-2.14.2.jar:/usr/share/java/cp-base-new/metrics-core-2.2.0.jar:/usr/share/java/cp-base-new/slf4j-reload4j-1.7.36.jar:/usr/share/java/cp-base-new/logredactor-metrics-1.0.12.jar:/usr/share/java/cp-base-new/reload4j-1.2.25.jar:/usr/share/java/cp-base-new/kafka-storage-api-7.4.3-ccs.jar:/usr/share/java/cp-base-new/json-simple-1.1.1.jar:/usr/share/java/cp-base-new/jackson-dataformat-yaml-2.14.2.jar:/usr/share/java/cp-base-new/re2j-1.6.jar:/usr/share/java/cp-base-new/lz4-java-1.8.0.jar:/usr/share/java/cp-base-new/kafka-raft-7.4.3-ccs.jar:/usr/share/java/cp-base-new/zstd-jni-1.5.2-1.jar:/usr/share/java/cp-base-new/logredactor-1.0.12.jar:/usr/share/java/cp-base-new/snakeyaml-2.0.jar:/usr/share/java/cp-base-new/jopt-simple-5.0.4.jar:/usr/share/java/cp-base-new/zookeeper-jute-3.6.3.jar:/usr/share/java/cp-base-new/jackson-annotations-2.14.2.jar:/usr/share/java/cp-base-new/jackson-dataformat-csv-2.14.2.jar:/usr/share/java/cp-base-new/kafka-server-common-7.4.3-ccs.jar:/usr/share/java/cp-base-new/gson-2.9.0.jar (org.apache.zookeeper.ZooKeeper)
   120	kafka-1          | [2025-08-15 09:10:04,278] INFO Client environment:java.library.path=/usr/java/packages/lib:/usr/lib64:/lib64:/lib:/usr/lib (org.apache.zookeeper.ZooKeeper)

### dotnet_test.log (excerpt)

     1	/bin/bash: line 1: dotnet: command not found

## Root Cause (hypothesis)
- docker compose environment failed to fully start (Kafka container exited).
- .NET SDK missing in runner environment (dotnet not found).

## Suggested Fix
- Install .NET SDK 8.x and ensure  is on PATH.
- Inspect Kafka container logs: .
- Verify resource limits and ports; re-run compose.

## Repro Steps
1. (cd physicalTests && docker compose up -d) | tee reportsx/physical/artifacts/compose_up.log
2. dotnet test physicalTests/Kafka.Ksql.Linq.Tests.Integration.csproj | tee reportsx/physical/artifacts/dotnet_test.log
3. (cd physicalTests && docker compose down) | tee reportsx/physical/artifacts/compose_down.log

## Next Action
- After fixing environment, re-run full flow.
- If Kafka still fails, capture  and  and classify again.
