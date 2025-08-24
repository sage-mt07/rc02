using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Kafka.Ksql.Linq.Core.Abstractions;

namespace Kafka.Ksql.Linq.Query.Adapters;

internal static class TopicNameResolver
{
    public static string Resolve(EntityModel model, string prefix, string? configured = null)
    {
        string name;
        if (!string.IsNullOrWhiteSpace(configured))
            name = configured!;
        else if (!string.IsNullOrWhiteSpace(model.TopicName))
            name = model.TopicName!;
        else if (model.AdditionalSettings.TryGetValue("topicCandidate", out var c) && c is string s && !string.IsNullOrWhiteSpace(s))
            name = s;
        else if (model.EntityType == typeof(object) && model.AdditionalSettings.TryGetValue("id", out var id) && id is string i)
            name = i;
        else
            name = prefix + model.EntityType.Name;
        return Sanitize(name);
    }

    private static string Sanitize(string n)
    {
        var name = Regex.Replace(n.ToLowerInvariant(), "[^a-z0-9_]", "_");
        if (name.Length > 249)
        {
            using var sha = SHA1.Create();
            var hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(name))).ToLowerInvariant();
            name = name[..230] + "_" + hash[..6];
        }
        return name;
    }
}
