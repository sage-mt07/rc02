namespace Kafka.Ksql.Linq.Mapping;

using Kafka.Ksql.Linq.Core.Models;
using Kafka.Ksql.Linq.Configuration;
using System;
using System.Reflection;
using Avro;
using Avro.Specific;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Globalization;

/// <summary>
/// Holds generated key/value types and their associated PropertyMeta information.
/// </summary>
internal class KeyValueTypeMapping
{
    public const char KeySep = '\u0000';
    public Type KeyType { get; set; } = default!;
    public PropertyMeta[] KeyProperties { get; set; } = Array.Empty<PropertyMeta>();
    public PropertyInfo[] KeyTypeProperties { get; set; } = Array.Empty<PropertyInfo>();

    public Type ValueType { get; set; } = default!;
    public PropertyMeta[] ValueProperties { get; set; } = Array.Empty<PropertyMeta>();
    public PropertyInfo[] ValueTypeProperties { get; set; } = Array.Empty<PropertyInfo>();

    // Avro specific-record types generated from KeyType and ValueType
    public Type? AvroKeyType { get; set; }
    public Type? AvroValueType { get; set; }

    // Avro schema json strings for key and value
    public string? AvroKeySchema { get; set; }
    public string? AvroValueSchema { get; set; }

    private static readonly ConcurrentDictionary<(Type poco, Type avro, string fp), Action<object, object>> PlanCache = new();

    /// <summary>
    /// Extract key object from POCO instance based on registered PropertyMeta.
    /// </summary>
    public object ExtractKey(object poco)
    {
        if (poco == null) throw new ArgumentNullException(nameof(poco));
        var keyInstance = Activator.CreateInstance(KeyType)!;
        for (int i = 0; i < KeyProperties.Length; i++)
        {
            var meta = KeyProperties[i];
            var value = meta.PropertyInfo!.GetValue(poco);
            KeyTypeProperties[i].SetValue(keyInstance, value);
        }
        return keyInstance;
    }

    /// <summary>
    /// Extract value object from POCO instance based on registered PropertyMeta.
    /// </summary>
    public object ExtractValue(object poco)
    {
        if (poco == null) throw new ArgumentNullException(nameof(poco));
        var valueInstance = Activator.CreateInstance(ValueType)!;
        for (int i = 0; i < ValueProperties.Length; i++)
        {
            var meta = ValueProperties[i];
            var value = meta.PropertyInfo!.GetValue(poco);
            ValueTypeProperties[i].SetValue(valueInstance, value);
        }
        return valueInstance;
    }

    /// <summary>
    /// Copy values from the provided POCO instance into the supplied
    /// key and value objects.
    /// </summary>
    /// <param name="poco">Source POCO instance.</param>
    /// <param name="key">Existing key object or null for keyless entities.</param>
    /// <param name="value">Existing value object to populate.</param>
    public void PopulateKeyValue(object poco, object? key, object value)
    {
        if (poco == null) throw new ArgumentNullException(nameof(poco));
        if (value == null) throw new ArgumentNullException(nameof(value));

        // copy value fields
        for (int i = 0; i < ValueProperties.Length; i++)
        {
            var meta = ValueProperties[i];
            var val = meta.PropertyInfo!.GetValue(poco);
            ValueTypeProperties[i].SetValue(value, val);
        }

        if (key != null)
        {
            for (int i = 0; i < KeyProperties.Length; i++)
            {
                var meta = KeyProperties[i];
                var val = meta.PropertyInfo!.GetValue(poco);
                KeyTypeProperties[i].SetValue(key, val);
            }
        }
    }

    /// <summary>
    /// Combine key and value objects into a POCO instance of the specified type.
    /// </summary>
    public object CombineFromKeyValue(object? key, object value, Type pocoType)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (pocoType == null) throw new ArgumentNullException(nameof(pocoType));

        var instance = Activator.CreateInstance(pocoType)!;
        try
        {
            // set value properties
            for (int i = 0; i < ValueProperties.Length; i++)
            {
                var meta = ValueProperties[i];
                var val = ValueTypeProperties[i].GetValue(value);
                meta.PropertyInfo!.SetValue(instance, val);
            }

            if (key != null)
            {
                for (int i = 0; i < KeyProperties.Length; i++)
                {
                    var meta = KeyProperties[i];
                    var val = KeyTypeProperties[i].GetValue(key);
                    meta.PropertyInfo!.SetValue(instance, val);
                }
            }

        }
        catch (Exception ex)
        {
            var ee = ex;
        }

        return instance;
    }

    /// <summary>
    /// Combine Avro specific-record key/value instances into a POCO using cached delegates.
    /// </summary>
    public object CombineFromAvroKeyValue(object? avroKey, object avroValue, Type pocoType)
    {
        if (avroValue is not ISpecificRecord vrec)
            throw new InvalidOperationException($"value must be ISpecificRecord. actual={avroValue.GetType()}");
        if (pocoType == null) throw new ArgumentNullException(nameof(pocoType));

        var vfp = Fingerprint(vrec.Schema);
        var vplan = PlanCache.GetOrAdd((pocoType, avroValue.GetType(), vfp), _ => BuildPlan(pocoType, vrec.Schema, ValueProperties));

        var instance = Activator.CreateInstance(pocoType)!;
        vplan(avroValue, instance);

        if (avroKey is ISpecificRecord krec)
        {
            var kfp = Fingerprint(krec.Schema);
            var kplan = PlanCache.GetOrAdd((pocoType, avroKey.GetType(), kfp), _ => BuildPlan(pocoType, krec.Schema, KeyProperties));
            kplan(avroKey, instance);
        }

        return instance;
    }

    public string FormatKeyForPrefix(object avroKey)
    {
        if (avroKey == null) throw new ArgumentNullException(nameof(avroKey));
        var parts = new string[KeyProperties.Length];
        for (int i = 0; i < KeyProperties.Length; i++)
        {
            var meta = KeyProperties[i];
            var p = avroKey.GetType().GetProperty(meta.PropertyInfo!.Name)
                    ?? throw new InvalidOperationException($"Key property '{meta.PropertyInfo!.Name}' not found on {avroKey.GetType().Name}");
            var raw = p.GetValue(avroKey);
            var targetType = meta.PropertyInfo.PropertyType;
            parts[i] = ToSortableString(raw, targetType);
        }
        return string.Join(KeySep, parts);
    }

    public object CombineFromStringKeyAndAvroValue(string key, object avroValue, Type pocoType)
    {
        if (pocoType == null) throw new ArgumentNullException(nameof(pocoType));
        if (avroValue is not ISpecificRecord vrec)
            throw new InvalidOperationException($"value must be ISpecificRecord. actual={avroValue.GetType()}");

        var vfp = Fingerprint(vrec.Schema);
        var vplan = PlanCache.GetOrAdd((pocoType, avroValue.GetType(), vfp), _ => BuildPlan(pocoType, vrec.Schema, ValueProperties));
        var instance = Activator.CreateInstance(pocoType)!;
        vplan(avroValue, instance);

        var parts = (key ?? string.Empty).Split(KeySep);
        for (int i = 0; i < KeyProperties.Length; i++)
        {
            var meta = KeyProperties[i];
            var prop = meta.PropertyInfo!;
            var str = i < parts.Length ? parts[i] : null;
            var val = FromKeyString(str, prop.PropertyType);
            prop.SetValue(instance, val);
        }
        return instance;
    }

    private static string ToSortableString(object? raw, Type targetType)
    {
        if (raw == null) return string.Empty;
        var t = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (t == typeof(DateTime))
        {
            DateTime utc;
            if (raw is long ms)
                utc = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
            else if (raw is DateTime dt)
                utc = dt.ToUniversalTime();
            else
                utc = Convert.ToDateTime(raw, CultureInfo.InvariantCulture).ToUniversalTime();
            return utc.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
        }
        if (t == typeof(Guid))
            return raw is Guid g ? g.ToString("D") : raw.ToString() ?? string.Empty;
        return Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static object? FromKeyString(string? s, Type targetType)
    {
        var t = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (string.IsNullOrEmpty(s))
        {
            if (t.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                return Activator.CreateInstance(t);
            return null;
        }
        if (t == typeof(DateTime))
        {
            if (DateTime.TryParseExact(s, "yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                return dt;
            return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();
        }
        if (t == typeof(Guid))
            return Guid.TryParse(s, out var g) ? g : Guid.Empty;
        if (t.IsEnum)
            return Enum.Parse(t, s, true);
        return Convert.ChangeType(s, t, CultureInfo.InvariantCulture);
    }

    private static Action<object, object> BuildPlan(Type pocoType, Schema schema, PropertyMeta[] metas)
    {
        if (schema is not RecordSchema rs)
            throw new InvalidOperationException($"Schema '{schema.Fullname}' is not RecordSchema.");

        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var f in rs.Fields)
        {
            map[f.Name] = f.Pos;
            if (f.Aliases != null)
            {
                foreach (var a in f.Aliases) map[a] = f.Pos;
            }
        }

        var positions = new int[metas.Length];
        for (int i = 0; i < metas.Length; i++)
        {
            var meta = metas[i];
            var avroName = meta.SourceName ?? meta.PropertyInfo!.Name;
            if (!map.TryGetValue(avroName, out var pos))
            {
                var alt = char.ToLowerInvariant(avroName[0]) + avroName.Substring(1);
                if (!map.TryGetValue(alt, out pos))
                    throw new InvalidOperationException($"Field '{avroName}' not found in schema '{rs.Fullname}' for POCO '{pocoType.Name}'");
            }
            positions[i] = pos;
        }

        var oAvro = Expression.Parameter(typeof(object), "avro");
        var oPoco = Expression.Parameter(typeof(object), "poco");
        var srec = Expression.Variable(typeof(ISpecificRecord), "s");
        var poco = Expression.Variable(pocoType, "p");
        var convM = typeof(KeyValueTypeMapping).GetMethod(nameof(ConvertIfNeeded), BindingFlags.NonPublic | BindingFlags.Static)!;
        var body = new List<Expression>
        {
            Expression.Assign(srec, Expression.Convert(oAvro, typeof(ISpecificRecord))),
            Expression.Assign(poco, Expression.Convert(oPoco, pocoType))
        };
        for (int i = 0; i < metas.Length; i++)
        {
            var prop = metas[i].PropertyInfo!;
            var get = Expression.Call(srec, typeof(ISpecificRecord).GetMethod("Get")!, Expression.Constant(positions[i]));
            var conv = Expression.Call(convM, get, Expression.Constant(prop.PropertyType, typeof(Type)));
            body.Add(Expression.Assign(Expression.Property(poco, prop), Expression.Convert(conv, prop.PropertyType)));
        }
        var lambda = Expression.Lambda<Action<object, object>>(Expression.Block(new[] { srec, poco }, body), oAvro, oPoco).Compile();
        return lambda;
    }

    private static string Fingerprint(Schema schema)
        => schema.ToString().GetHashCode().ToString("X");

    private static object? ConvertIfNeeded(object? raw, Type targetType)
    {
        if (raw is null) return null;
        var t = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (t.IsInstanceOfType(raw)) return raw;
        if (t == typeof(DateTime) && raw is long ms)
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        if (t == typeof(decimal))
        {
            if (raw is AvroDecimal adv) return (decimal)adv;
            if (raw is decimal d) return d;
            try { return Convert.ChangeType(raw, t); } catch { }
        }
        if (t == typeof(Guid) && raw is string sg && Guid.TryParse(sg, out var g))
            return g;
        try { return Convert.ChangeType(raw, t); }
        catch { return raw; }
    }

    private static AvroDecimal ToAvroDecimal(decimal value, int scale) =>
        new AvroDecimal(decimal.Parse(decimal.Round(value, scale).ToString($"F{scale}", CultureInfo.InvariantCulture)));

    public object ExtractAvroKey(object poco)
    {
        if (poco == null) throw new ArgumentNullException(nameof(poco));
        if (AvroKeyType == null) throw new InvalidOperationException("AvroKeyType not registered");
        var keyInstance = Activator.CreateInstance(AvroKeyType)!;
        for (int i = 0; i < KeyProperties.Length; i++)
        {
            var meta = KeyProperties[i];
            var value = meta.PropertyInfo!.GetValue(poco);
            var avroProp = AvroKeyType!.GetProperty(meta.PropertyInfo!.Name)!;
            var scale = DecimalPrecisionConfig.ResolveScale(meta.Scale, meta.PropertyInfo);
            var avroType = avroProp.PropertyType;
            var isNullableAvroDecimal = avroType.IsGenericType && avroType.GetGenericTypeDefinition() == typeof(Nullable<>) && avroType.GetGenericArguments()[0] == typeof(AvroDecimal);
            if (isNullableAvroDecimal && value is null)
            {
                avroProp.SetValue(keyInstance, null);
            }
            else if ((avroType == typeof(AvroDecimal) || isNullableAvroDecimal) && value is decimal decKey)
            {
                avroProp.SetValue(keyInstance, ToAvroDecimal(decKey, scale));
            }
            else if (avroProp.PropertyType == typeof(string) && value is Guid g)
                avroProp.SetValue(keyInstance, g.ToString("D"));
            else if (avroProp.PropertyType == typeof(double) && value is float f)
                avroProp.SetValue(keyInstance, (double)f);
            else
                avroProp.SetValue(keyInstance, value);
        }
        return keyInstance;
    }

    public object ExtractAvroValue(object poco)
    {
        if (poco == null) throw new ArgumentNullException(nameof(poco));
        if (AvroValueType == null) throw new InvalidOperationException("AvroValueType not registered");
        var valueInstance = Activator.CreateInstance(AvroValueType)!;
        for (int i = 0; i < ValueProperties.Length; i++)
        {
            var meta = ValueProperties[i];
            var value = meta.PropertyInfo!.GetValue(poco);
            var avroProp = AvroValueType!.GetProperty(meta.PropertyInfo!.Name)!;
            var scale = DecimalPrecisionConfig.ResolveScale(meta.Scale, meta.PropertyInfo);
            var avroTypeVal = avroProp.PropertyType;
            var isNullableAvroDecimalVal = avroTypeVal.IsGenericType && avroTypeVal.GetGenericTypeDefinition() == typeof(Nullable<>) && avroTypeVal.GetGenericArguments()[0] == typeof(AvroDecimal);
            if (isNullableAvroDecimalVal && value is null)
            {
                avroProp.SetValue(valueInstance, null);
            }
            else if ((avroTypeVal == typeof(AvroDecimal) || isNullableAvroDecimalVal) && value is decimal decVal)
            {
                avroProp.SetValue(valueInstance, ToAvroDecimal(decVal, scale));
            }
            else if (avroProp.PropertyType == typeof(string) && value is Guid g)
                avroProp.SetValue(valueInstance, g.ToString("D"));
            else if (avroProp.PropertyType == typeof(double) && value is float fv)
                avroProp.SetValue(valueInstance, (double)fv);
            else
                avroProp.SetValue(valueInstance, value);
        }
        return valueInstance;
    }

    public void PopulateAvroKeyValue(object poco, object? key, object value)
    {
        if (poco == null) throw new ArgumentNullException(nameof(poco));
        if (value == null) throw new ArgumentNullException(nameof(value));

        for (int i = 0; i < ValueProperties.Length; i++)
        {
            var meta = ValueProperties[i];
            var val = meta.PropertyInfo!.GetValue(poco);
            var avroProp = AvroValueType!.GetProperty(meta.PropertyInfo!.Name)!;
            var scale = DecimalPrecisionConfig.ResolveScale(meta.Scale, meta.PropertyInfo);
            var avroTypeValueProp = avroProp.PropertyType;
            var isNullableAvroDecimalValueProp = avroTypeValueProp.IsGenericType && avroTypeValueProp.GetGenericTypeDefinition() == typeof(Nullable<>) && avroTypeValueProp.GetGenericArguments()[0] == typeof(AvroDecimal);
            if (isNullableAvroDecimalValueProp && val is null)
            {
                avroProp.SetValue(value, null);
            }
            else if ((avroTypeValueProp == typeof(AvroDecimal) || isNullableAvroDecimalValueProp) && val is decimal decv)
            {
                avroProp.SetValue(value, ToAvroDecimal(decv, scale));
            }
            else if (avroProp.PropertyType == typeof(string) && val is Guid gv)
                avroProp.SetValue(value, gv.ToString("D"));
            else if (avroProp.PropertyType == typeof(double) && val is float fvv)
                avroProp.SetValue(value, (double)fvv);
            else
                avroProp.SetValue(value, val);
        }

        if (key != null)
        {
            for (int i = 0; i < KeyProperties.Length; i++)
            {
                var meta = KeyProperties[i];
                var val = meta.PropertyInfo!.GetValue(poco);
                var avroProp = AvroKeyType!.GetProperty(meta.PropertyInfo!.Name)!;
                var scale = DecimalPrecisionConfig.ResolveScale(meta.Scale, meta.PropertyInfo);
                var avroTypeKeyProp = avroProp.PropertyType;
                var isNullableAvroDecimalKeyProp = avroTypeKeyProp.IsGenericType && avroTypeKeyProp.GetGenericTypeDefinition() == typeof(Nullable<>) && avroTypeKeyProp.GetGenericArguments()[0] == typeof(AvroDecimal);
                if (isNullableAvroDecimalKeyProp && val is null)
                    avroProp.SetValue(key, null);
                else if ((avroTypeKeyProp == typeof(AvroDecimal) || isNullableAvroDecimalKeyProp) && val is decimal dek)
                {
                    avroProp.SetValue(key, ToAvroDecimal(dek, scale));
                }
                else if (avroProp.PropertyType == typeof(string) && val is Guid gk)
                    avroProp.SetValue(key, gk.ToString("D"));
                else if (avroProp.PropertyType == typeof(double) && val is float fk)
                    avroProp.SetValue(key, (double)fk);
                else
                    avroProp.SetValue(key, val);
            }
        }
    }
}
