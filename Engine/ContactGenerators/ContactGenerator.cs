using Engine.Collision;

namespace Engine.ContactGenerators;

/// <summary>
/// Basic interface for all contact generators.
/// </summary>
public interface IContactGenerator
{
    // TODO: consider adding a limit parameter to restrict the number of contacts added
    /// <summary>
    /// Add contacts to be resolved if the constraint associated with the generator is violated.
    /// </summary>
    /// <param name="data">The <see cref="CollisionData"/> to which the generated contacts will be added.</param>
    /// <returns>The number of contacts added. </returns>
    public uint AddContacts(CollisionData data);
}