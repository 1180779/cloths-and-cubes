using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

public static class ObjectInspectorWindow
{
    public static void Draw(object?[] objects)
    {
        ImGui.Begin("Object Inspector");

        // Use per-root visited sets to avoid infinite loops on cyclic graphs
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] is null) continue;
            var obj = objects[i];
            ImGui.PushID(i);
            DrawNode($"{RuntimeHelpers.GetHashCode(obj),10}: {obj?.GetType().Name ?? "null"}", obj,
                new HashSet<object>(ReferenceEqualityComparer.Instance));
            ImGui.PopID();
        }

        ImGui.End();
    }

    /// <summary>
    /// Recursively render an object graph using reflection and handle arbitrary depth
    /// </summary>
    /// <param name="label"></param>
    /// <param name="value"></param>
    /// <param name="visited"></param>
    private static void DrawNode(string label, object? value, HashSet<object> visited)
    {
        // null or simple types: render inline
        if (value is null)
        {
            ImGui.Text($"{label}: null");
            return;
        }

        var type = value.GetType();
        if (IsLeaf(type))
        {
            if (value is double d)
                ImGui.Text($"{label}: {(d >= 0 ? " " : "")}{d:F2}");
            else if (value is float f)
                ImGui.Text($"{label}: {(f >= 0 ? " " : "")}{f:F2}");
            else
                ImGui.Text($"{label}: {ToInlineString(value)}");
            return;
        }

        // Handle IEnumerable (but not string, already handled as leaf)
        if (value is IEnumerable enumerable)
        {
            if (ImGui.TreeNode($"{label} (IEnumerable<{type.Name}>)"))
            {
                int idx = 0;
                foreach (var item in enumerable)
                {
                    ImGui.PushID(idx);
                    DrawNode($"[{idx}]", item, visited);
                    ImGui.PopID();
                    idx++;
                }

                ImGui.TreePop();
            }

            return;
        }

        // Prevent cycles for reference types
        if (!type.IsValueType)
        {
            if (!visited.Add(value))
            {
                ImGui.Text($"{label}: <cyclic reference>");
                return;
            }
        }

        // Complex object: show a collapsible node and recurse over public properties and fields
        if (ImGui.TreeNode($"{label} <{type.Name}>"))
        {
            // Properties
            foreach (var prop in SafeGetProperties(type))
            {
                if (prop.GetIndexParameters().Length > 0) continue; // skip indexers

                object? propVal;
                try
                {
                    propVal = prop.GetValue(value);
                }
                catch
                {
                    continue; // skip properties that throw
                }

                ImGui.PushID(prop.Name);
                DrawNode(prop.Name, propVal, visited);
                ImGui.PopID();
            }

            // Fields
            foreach (var field in SafeGetFields(type))
            {
                object? fieldVal;
                try
                {
                    fieldVal = field.GetValue(value);
                }
                catch
                {
                    continue; // skip fields that throw
                }

                ImGui.PushID(field.Name);
                DrawNode(field.Name, fieldVal, visited);
                ImGui.PopID();
            }

            ImGui.TreePop();
        }

        // Allow the same object to appear through another path (optional). Remove if you want "visited" to be global.
        if (!type.IsValueType)
        {
            visited.Remove(value);
        }
    }

    private static bool IsLeaf(Type t)
    {
        // Treat primitives, enums, strings, decimals and DateTime/TimeSpan/Guid as leaf values
        if (t.IsPrimitive || t.IsEnum) return true;
        if (t == typeof(string) || t == typeof(decimal)) return true;
        if (t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(TimeSpan) ||
            t == typeof(Guid)) return true;
        return false;
    }

    private static string ToInlineString(object value)
    {
        try
        {
            return value.ToString() ?? "<null>";
        }
        catch
        {
            return "<unprintable>";
        }
    }

    private static IEnumerable<PropertyInfo> SafeGetProperties(Type t)
    {
        try
        {
            return t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        }
        catch
        {
            // Some runtime types can throw here; return empty to be safe
            return [];
        }
    }

    private static IEnumerable<FieldInfo> SafeGetFields(Type t)
    {
        try
        {
            return t.GetFields(BindingFlags.Instance | BindingFlags.Public);
        }
        catch
        {
            return [];
        }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}