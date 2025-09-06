using System.Reflection;

namespace Visualisation.Core.FrameCapsule;

public static class ObjectStateRestorer
{
    /// <summary>
    /// *   Restores the state of a target object from a source object.
    /// *   It recursively copies all field values from the source to the target.
    /// *   This modifies the target object in-place, preserving its identity (reference).
    /// </summary>
    /// <typeparam name="T">The type of the objects.</typeparam>
    /// <param name="target">The object to be modified (e.g., the live object).</param>
    /// <param name="source">The object to read data from (e.g., the snapshot).</param>
    public static void RestoreStateFrom<T>(T target, T source) where T : class
    {
        if (target == null || source == null)
        {
            // Or throw an exception, depending on desired behavior
            return;
        }

        RestoreStateInternal(
            target,
            source,
            new Dictionary<object, object>(ReferenceEqualityComparer.Instance)
        );
    }

    private static void RestoreStateInternal(
        object target,
        object source,
        IDictionary<object, object> visited
    )
    {
        // Basic validation
        if (target == null || source == null)
            return;

        var type = source.GetType();
        if (target.GetType() != type)
        {
            throw new ArgumentException(
                $"Target and source types do not match: {target.GetType()} vs {type}"
            );
        }

        // Handle primitives, strings, etc. - their value is copied by the parent's SetValue call.
        if (
            type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
        )
        {
            return;
        }

        // Handle circular references: if we've already restored this source object, stop.
        if (visited.ContainsKey(source))
        {
            return;
        }

        // Mark this source/target pair as visited.
        visited[source] = target;

        // Handle arrays
        if (type.IsArray)
        {
            var sourceArray = (Array)source;
            var targetArray = (Array)target;

            if (targetArray.Length != sourceArray.Length)
            {
                // We cannot resize an existing array. This is a limitation of this approach.
                // The object graph structure must be identical.
                // Consider logging a warning or throwing an exception.
                return;
            }

            for (int i = 0; i < sourceArray.Length; i++)
            {
                var sourceElement = sourceArray.GetValue(i);
                var targetElement = targetArray.GetValue(i);

                if (sourceElement == null || targetElement == null)
                {
                    // Overwrite with the source value (which might be null)
                    targetArray.SetValue(sourceElement, i);
                }
                else if (sourceElement.GetType().IsValueType)
                {
                    // For structs and primitives, just copy the value.
                    targetArray.SetValue(sourceElement, i);
                }
                else
                {
                    // For reference types, recurse.
                    RestoreStateInternal(targetElement, sourceElement, visited);
                }
            }

            return;
        }

        // Handle reference types (classes) and structs
        foreach (var field in GetAllFields(type))
        {
            var sourceValue = field.GetValue(source);
            var targetValue = field.GetValue(target);

            if (sourceValue == null)
            {
                // If the source's field is null, set the target's field to null.
                field.SetValue(target, null);
            }
            else if (
                sourceValue.GetType().IsValueType
                || sourceValue.GetType() == typeof(string)
            )
            {
                // For value types (structs, primitives) and strings, directly set the value.
                field.SetValue(target, sourceValue);
            }
            else
            {
                // For other reference types, recurse into them.
                if (targetValue != null)
                {
                    RestoreStateInternal(targetValue, sourceValue, visited);
                }
                // If targetValue is null but sourceValue is not, we have a structural
                // mismatch. The snapshot has an object where the live scene has null.
                // We cannot "restore" into a null reference. This is a key limitation.
            }
        }
    }

    /// <summary>
    /// Gets all fields for a type, including those from its base classes.
    /// </summary>
    private static IEnumerable<FieldInfo> GetAllFields(Type type)
    {
        var fields = new List<FieldInfo>();
        while (type != null && type != typeof(object))
        {
            fields.AddRange(
                type.GetFields(
                    BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.DeclaredOnly
                )
            );
            type = type.BaseType!;
        }

        return fields;
    }
}