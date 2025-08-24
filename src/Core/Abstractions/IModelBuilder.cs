namespace Kafka.Ksql.Linq.Core.Abstractions;

public interface IModelBuilder
{
    /// <summary>
    /// Registers an entity type in the model and optionally marks it as read-only or write-only.
    /// </summary>
    /// <param name="readOnly">If true, the entity is used only for reads.</param>
    /// <param name="writeOnly">If true, the entity is used only for writes.</param>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <returns>An <see cref="IEntityBuilder{T}"/> for further configuration.</returns>
    IEntityBuilder<T> Entity<T>(bool readOnly = false, bool writeOnly = false) where T : class;
}
