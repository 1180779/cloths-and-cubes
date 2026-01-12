using Engine;

using ImGuiNET;

namespace Visualization.UiLayer.UI.Windows;

#if DEBUG

/// <summary>
/// A specialized inspector window for Contact arrays that filters and displays only valid contacts
/// (where both Body elements are not null)
/// </summary>
public sealed class ContactsInspectorWindow(Func<Contact[]> contactProvider) : IWindow
{
    private Func<Contact[]> _contactProvider = contactProvider;

    private static readonly System.Numerics.Vector4 ValidContactColor = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly System.Numerics.Vector4 InvalidContactColor = new(0.9f, 0.3f, 0.3f, 1.0f);
    private static readonly System.Numerics.Vector4 InfoColor = new(0.7f, 0.7f, 0.9f, 1.0f);

    public const string WindowName = "Contacts Inspector";
    public string Name => WindowName;

    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin("Contacts Inspector", ref isOpen))
        {
            DrawWindowContent(_contactProvider(), false);
        }

        ImGui.End();
    }

    /// <summary>
    /// Draws the contacts inspector window, showing only contacts where both bodies are not null
    /// </summary>
    /// <param name="contacts">Array of contacts to inspect</param>
    /// <param name="showInvalidContacts">If true, also displays contacts with null bodies (grayed out)</param>
    private static void DrawWindowContent(
        Contact[]? contacts,
        bool showInvalidContacts = false)
    {
        if (contacts == null || contacts.Length == 0)
        {
            ImGui.TextDisabled("No contacts available");
            return;
        }

        // Filter valid contacts (both bodies not null)
        var validContacts = contacts
            .Where(c => c != null && c.Body[0] != null && c.Body[1] != null)
            .ToArray();

        var invalidCount = contacts.Length - validContacts.Length;

        // Display statistics
        ImGui.TextColored(InfoColor, $"Total Contacts: {contacts.Length}");
        ImGui.SameLine();
        ImGui.TextColored(ValidContactColor, $"Valid: {validContacts.Length}");

        if (invalidCount > 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(InvalidContactColor, $"Invalid: {invalidCount}");
        }

        ImGui.Separator();
        ImGui.Spacing();

        // Show/hide invalid contacts toggle
        if (invalidCount > 0)
        {
            ImGui.Checkbox("Show invalid contacts", ref showInvalidContacts);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        // Display valid contacts using the ObjectInspectorWindow logic
        if (validContacts.Length > 0)
        {
            ImGui.TextColored(ValidContactColor, "Valid Contacts (Both bodies present):");
            ImGui.Spacing();

            // Convert to object array for the inspector
            object?[] validContactObjects = validContacts.Cast<object?>().ToArray();
            DrawContactsSection(validContactObjects);
        }
        else
        {
            ImGui.TextDisabled("No valid contacts to display");
        }

        // Optionally display invalid contacts
        if (showInvalidContacts && invalidCount > 0)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(InvalidContactColor, "Invalid Contacts (Missing body references):");
            ImGui.Spacing();

            var invalidContacts = contacts
                .Where(c => c == null || c.Body[0] == null || c.Body[1] == null)
                .Cast<object?>()
                .ToArray();

            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.6f); // Make invalid contacts more transparent
            DrawContactsSection(invalidContacts);
            ImGui.PopStyleVar();
        }
    }

    /// <summary>
    /// Draws a section of contacts using the enhanced ObjectInspectorWindow styling
    /// </summary>
    private static void DrawContactsSection(object?[] contacts)
    {
        var headerColor = new System.Numerics.Vector4(0.2f, 0.5f, 0.8f, 1.0f);
        var separatorColor = new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 0.5f);

        for (int i = 0; i < contacts.Length; i++)
        {
            if (contacts[i] is null)
            {
                ImGui.PushID(i);
                ImGui.TextDisabled($"[{i}]: null");
                ImGui.PopID();

                if (i < contacts.Length - 1)
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                }

                continue;
            }

            var contact = contacts[i] as Contact;
            ImGui.PushID(i);

            // Build a descriptive label for the contact
            var contactLabel = BuildContactLabel(i, contact);

            ImGui.PushStyleColor(ImGuiCol.Header, headerColor);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new System.Numerics.Vector4(0.3f, 0.6f, 0.9f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new System.Numerics.Vector4(0.25f, 0.55f, 0.85f, 1.0f));

            var isOpen = ImGui.CollapsingHeader(contactLabel, ImGuiTreeNodeFlags.DefaultOpen);

            ImGui.PopStyleColor(3);

            if (isOpen)
            {
                ImGui.Indent();
                DrawContactDetails(contact);
                ImGui.Unindent();
            }

            ImGui.PopID();

            // Add visual separator between contacts
            if (i < contacts.Length - 1)
            {
                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Separator, separatorColor);
                ImGui.Separator();
                ImGui.PopStyleColor();
                ImGui.Spacing();
            }
        }
    }

    /// <summary>
    /// Builds a descriptive label for a contact header
    /// </summary>
    private static string BuildContactLabel(int index, Contact? contact)
    {
        if (contact == null)
            return $"[{index}] Contact: null";

        var body0Type = contact.Body[0]?.GetType().Name ?? "null";
        var body1Type = contact.Body[1]?.GetType().Name ?? "null";
        var penetration = contact.Penetration;

        return $"[{index}] Contact: {body0Type} ↔ {body1Type} (Penetration: {penetration:F3})";
    }

    /// <summary>
    /// Draws detailed information about a contact
    /// </summary>
    private static void DrawContactDetails(Contact? contact)
    {
        if (contact == null)
        {
            ImGui.TextDisabled("null contact");
            return;
        }

        // Display key contact properties in a more readable format
        ImGui.TextColored(new System.Numerics.Vector4(0.6f, 0.6f, 0.9f, 1.0f), "Contact Properties:");
        ImGui.Indent(10);

        ImGui.Text($"Penetration: {contact.Penetration:F3}");
        ImGui.Text($"Friction: {contact.Friction:F3}");
        ImGui.Text($"Restitution: {contact.Restitution:F3}");

        ImGui.Text(
            $"Contact Point: ({contact.ContactPoint.X:F2}, {contact.ContactPoint.Y:F2}, {contact.ContactPoint.Z:F2})");
        ImGui.Text(
            $"Contact Normal: ({contact.ContactNormal.X:F2}, {contact.ContactNormal.Y:F2}, {contact.ContactNormal.Z:F2})");

        ImGui.Unindent(10);
        ImGui.Spacing();

        // Display bodies
        ImGui.TextColored(new System.Numerics.Vector4(0.9f, 0.6f, 0.6f, 1.0f), "Bodies:");
        ImGui.Indent(10);

        for (int i = 0; i < 2; i++)
        {
            var body = contact.Body[i];
            if (body != null)
            {
                ImGui.PushID($"Body{i}");
                if (ImGui.TreeNode($"Body[{i}] ({body.GetType().Name})"))
                {
                    // Use ObjectInspectorWindow's DrawNode for detailed inspection
                    var visited = new HashSet<object>(
                        ObjectInspectorWindow.ReferenceEqualityComparer.Instance);
                    ObjectInspectorWindow.DrawNode(null, body, visited, 0);
                    ImGui.TreePop();
                }

                ImGui.PopID();
            }
            else
            {
                ImGui.TextDisabled($"Body[{i}]: null");
            }
        }

        ImGui.Unindent(10);
    }
}

#endif