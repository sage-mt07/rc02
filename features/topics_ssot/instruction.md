# appsettings Topics SSOT
- base key `<namespace>.<ClassName>` defines NumPartitions and ReplicationFactor
- child keys `.pub` and `.int` may only contain Producer/Consumer configs
- structure parameters in child sections result in errors
- base structure applies equally to both `.pub` and `.int`
