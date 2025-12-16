using Engine.RigidBodies;

namespace Engine;

/// <summary>
/// Stores a potential contact to check later.
/// </summary>
struct PotentialContact
{
    /* Holds the bodies that might be in contact.
       Ensure body has only 0 and 1 in the tab */
    private RigidBody[] body;
};