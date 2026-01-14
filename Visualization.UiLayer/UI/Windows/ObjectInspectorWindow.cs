using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

using ImGuiNET;

using Visualisation.Core.GameObjects;

namespace Visualization.UiLayer.UI.Windows;

public sealed class ObjectInspectorWindow(Func<IEnumerable<GameObject>> gameObjectProvider) : IWindow
{
    private readonly Func<IEnumerable<GameObject>> _gameObjectProvider = gameObjectProvider;

    private static readonly System.Numerics.Vector4 HeaderColor = new(0.2f, 0.5f, 0.8f, 1.0f);
    private static readonly System.Numerics.Vector4 SeparatorColor = new(0.5f, 0.5f, 0.5f, 0.5f);

    public const string StaticName = "Object Inspector";
    public string Name => StaticName;

    public void Draw(ref bool isOpen)
    {
        if (!isOpen) return;

        DrawInternal(_gameObjectProvider().Select(g => g.PhysicsObject).ToArray(), ref isOpen);
    }

    private void DrawInternal(object?[] objects, ref bool isOpen)
    {
        if (ImGui.Begin(Name, ref isOpen))
        {
            ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                $"Total Objects: {objects.Length}".ToString());
            ImGui.Separator();
            ImGui.Spacing();

            // Use per-root visited sets to avoid infinite loops on cyclic graphs
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] is null)
                {
                    ImGui.PushID(i);
                    ImGui.TextDisabled($"[{i}]: null");
                    ImGui.PopID();

                    if (i < objects.Length - 1)
                    {
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                    }

                    continue;
                }

                var obj = objects[i];
                ImGui.PushID(i);

                // Draw a collapsible header for each top-level object with visual styling
                var typeName = obj?.GetType().Name ?? "null";
                var hashCode = RuntimeHelpers.GetHashCode(obj);

                ImGui.PushStyleColor(ImGuiCol.Header, HeaderColor);
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new System.Numerics.Vector4(0.3f, 0.6f, 0.9f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, new System.Numerics.Vector4(0.25f, 0.55f, 0.85f, 1.0f));

                var isHeaderOpen =
                    ImGui.CollapsingHeader($"[{i}] {typeName} (ID: {hashCode})", ImGuiTreeNodeFlags.DefaultOpen);

                ImGui.PopStyleColor(3);

                if (isHeaderOpen)
                {
                    ImGui.Indent();
                    DrawNode(null, obj, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
                    ImGui.Unindent();
                }

                ImGui.PopID();

                // Add visual separator between objects
                if (i < objects.Length - 1)
                {
                    ImGui.Spacing();
                    ImGui.PushStyleColor(ImGuiCol.Separator, SeparatorColor);
                    ImGui.Separator();
                    ImGui.PopStyleColor();
                    ImGui.Spacing();
                }
            }
        }

        ImGui.End();
    }

    /// <summary>
    /// Recursively render an object graph using reflection and handle arbitrary depth
    /// </summary>
    /// <param name="label"></param>
    /// <param name="value"></param>
    /// <param name="visited"></param>
    /// <param name="depth"></param>
    internal static void DrawNode(string? label, object? value, HashSet<object> visited, int depth)
    {
        // null or simple types: render inline
        if (value is null)
        {
            if (label != null)
                ImGui.TextDisabled($"{label}: null");
            else
                ImGui.TextDisabled("null");
            return;
        }

        var type = value.GetType();
        if (IsLeaf(type))
        {
            var prefix = label != null ? $"{label}: " : "";

            if (value is double d)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.8f, 0.4f, 1.0f),
                    $"{prefix}{(d >= 0 ? " " : "")}{d:F2}");
            }
            else if (value is float f)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.8f, 0.4f, 1.0f),
                    $"{prefix}{(f >= 0 ? " " : "")}{f:F2}");
            }
            else if (value is int or long or short or byte)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.9f, 0.6f, 1.0f),
                    $"{prefix}{ToInlineString(value)}");
            }
            else if (value is bool)
            {
                var boolColor = (bool)value
                    ? new System.Numerics.Vector4(0.3f, 0.9f, 0.3f, 1.0f)
                    : new System.Numerics.Vector4(0.9f, 0.3f, 0.3f, 1.0f);
                ImGui.TextColored(boolColor, $"{prefix}{ToInlineString(value)}");
            }
            else if (value is string)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.9f, 0.7f, 0.4f, 1.0f),
                    $"{prefix}\"{ToInlineString(value)}\"");
            }
            else
            {
                ImGui.Text($"{prefix}{ToInlineString(value)}");
            }

            return;
        }

        // Handle IEnumerable (but not string, already handled as leaf)
        if (value is IEnumerable enumerable)
        {
            var displayLabel = label ?? "Collection";
            var nodeLabel = $"{displayLabel} ({type.Name})";

            if (ImGui.TreeNode(nodeLabel))
            {
                int idx = 0;
                foreach (var item in enumerable)
                {
                    ImGui.PushID(idx);
                    DrawNode($"[{idx}]", item, visited, depth + 1);
                    ImGui.PopID();
                    idx++;
                }

                if (idx == 0)
                {
                    ImGui.TextDisabled("  (empty)");
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
                var displayLabel = label ?? "Object";
                ImGui.TextColored(new System.Numerics.Vector4(0.9f, 0.5f, 0.2f, 1.0f),
                    $"{displayLabel}: <cyclic reference>");
                return;
            }
        }

        // Complex object: show a collapsible node and recurse over public properties and fields
        var objectLabel = label != null ? $"{label} ({type.Name})" : type.Name;

        if (ImGui.TreeNode(objectLabel))
        {
            var properties = SafeGetProperties(type).ToList();
            var fields = SafeGetFields(type).ToList();

            // Properties
            if (properties.Count > 0)
            {
                ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.6f, 0.9f, 1.0f), "Properties:");
                ImGui.Indent(10);

                foreach (var prop in properties)
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
                    DrawNode(prop.Name, propVal, visited, depth + 1);
                    ImGui.PopID();
                }

                ImGui.Unindent(10);
            }

            // Fields
            if (fields.Count > 0)
            {
                if (properties.Count > 0)
                    ImGui.Spacing();

                ImGui.TextColored(new System.Numerics.Vector4(0.9f, 0.6f, 0.6f, 1.0f), "Fields:");
                ImGui.Indent(10);

                foreach (var field in fields)
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
                    DrawNode(field.Name, fieldVal, visited, depth + 1);
                    ImGui.PopID();
                }

                ImGui.Unindent(10);
            }

            if (properties.Count == 0 && fields.Count == 0)
            {
                ImGui.TextDisabled("  (no public members)");
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

    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}