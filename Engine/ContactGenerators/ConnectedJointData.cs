namespace Engine.ContactGenerators;

/// <summary>
/// The structure used to record the joint to which a rigid body is connected.
/// Can be used to track connected joints by a body and facilitate the removal
/// of the joints when needed ex. one of the connected bodies is removed. 
/// </summary>
public sealed record ConnectedJointData
{
    /// <summary>
    /// The joint to which the rigid body is connected.
    /// </summary>
    public Joint? Joint { get; private set; }

    /// <summary>
    /// The index of the joint in the global joint list of the physics engine.
    /// -1 if not set.
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// Sets the joint and its index.
    /// </summary>
    /// <param name="joint"></param>
    /// <param name="index"></param>
    public void Set(Joint joint, int index)
    {
        Joint = joint;
        Index = index;
    }
}