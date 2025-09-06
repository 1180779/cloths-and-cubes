using System.Reflection;
using System.Runtime.Serialization;

namespace Visualisation.Core.FrameCapsule;

public static class ObjectCloner
{
    public static T? CreateDeepCopy<T>(T? original)
    {
        return (T?)CreateDeepCopyInternal(
            original,
            new Dictionary<object, object>(ReferenceEqualityComparer.Instance)
        );
    }

    private static object? CreateDeepCopyInternal(
        object? originalObject,
        IDictionary<object, object> visited
    )
    {
        if (originalObject is null)
            return null;

        var type = originalObject.GetType();
        if (
            type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
        )
        {
            return originalObject;
        }

        if (type.IsValueType)
        {
            return CopyStruct(originalObject, visited);
        }

        if (visited.TryGetValue(originalObject, out var existingCopy))
        {
            return existingCopy;
        }

        if (type.IsArray)
        {
            return CopyArray((Array)originalObject, visited);
        }

        return CopyObject(originalObject, visited);
    }

    /// <summary>
    /// Gets all fields for a type, including those from its base classes.
    /// </summary>
    private static IEnumerable<FieldInfo> GetAllFields(Type type)
    {
        var fields = new List<FieldInfo>();
        // Walk the inheritance chain
        while (type != null && type != typeof(object))
        {
            fields.AddRange(
                type.GetFields(
                    BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.DeclaredOnly // Important: Only get fields declared on this type
                )
            );
            type = type.BaseType!;
        }

        return fields;
    }

    private static object CopyStruct(
        object originalObject,
        IDictionary<object, object> visited
    )
    {
        object copy = Activator.CreateInstance(originalObject.GetType())!;

        // Use the new helper to get all fields, including inherited ones.
        foreach (var field in GetAllFields(originalObject.GetType()))
        {
            var fieldValue = field.GetValue(originalObject);
            field.SetValue(copy, CreateDeepCopyInternal(fieldValue, visited));
        }

        return copy;
    }

    private static object CopyArray(
        Array originalArray,
        IDictionary<object, object> visited
    )
    {
        var type = originalArray.GetType();
        var elementType = type.GetElementType()!;
        var newArray = Array.CreateInstance(elementType, originalArray.Length);

        visited[originalArray] = newArray;

        for (int i = 0; i < originalArray.Length; i++)
        {
            var element = originalArray.GetValue(i);
            newArray.SetValue(CreateDeepCopyInternal(element, visited), i);
        }

        return newArray;
    }

    private static object CopyObject(
        object originalObject,
        IDictionary<object, object> visited
    )
    {
        var type = originalObject.GetType();
        object copy = FormatterServices.GetUninitializedObject(type);

        visited[originalObject] = copy;

        // Use the new helper to get all fields, including inherited ones.
        foreach (var field in GetAllFields(type))
        {
            var fieldValue = field.GetValue(originalObject);
            field.SetValue(copy, CreateDeepCopyInternal(fieldValue, visited));
        }

        return copy;
    }
}