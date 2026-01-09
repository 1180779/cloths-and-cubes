namespace Engine.ContactGenerators;

/// <summary>
/// The structure used to record the joint to which a rigid body is connected.
/// Can be used to track connected joints by a body and facilitate the removal
/// of the joints when needed ex. one of the connected bodies is removed.
///
/// <note>
/// Since this is a struct clear and set it with new values to avoid
/// common pitfalls with struct properties (which are temporarily copied when accessed).
/// </note>
/// </summary>
public struct ConnectedJointData
{
    public ConnectedJointData()
    {
        Joint = null;
        Index = -1;
    }

    public ConnectedJointData(Joint joint, int index)
    {
        Joint = joint;
        Index = index;
    }

    /// <summary>
    /// The joint to which the rigid body is connected.
    /// </summary>
    public Joint? Joint { get; private set; }

    /// <summary>
    /// The index of the joint in the global joint list of the physics engine.
    /// -1 if not set.
    /// </summary>
    public int Index { get; private set; }

    public bool IsSet => Joint is not null && Index >= 0;

    /// <summary>
    /// Updates the index of the joint. Useful when the joint's position
    /// in the global list changes (e.g., after a swap-and-pop operation).
    /// <param name="newIndex"></param>
    /// </summary>
    /// <note>
    /// Please remember to set the entire struct back to the property
    /// after calling this method, as struct properties are copied when accessed. 
    /// </note>
    public void SetIndex(int newIndex)
    {
        Index = newIndex;
    }
}