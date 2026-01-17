namespace Engine.ContactGenerators;

/// <summary>
/// Interface for bodies that can be tracked by joints for index updates.
/// 
/// This allows joints to update ConnectedJointData indices in both connected objects
/// after swap-and-pop operations in the global joint list.
/// </summary>
public interface IJointTrackable
{
    /// <summary>
    /// Updates the index of a specific joint in this object's tracking data.
    /// </summary>
    /// <param name="joint">The joint whose index needs to be updated.</param>
    /// <param name="newIndex">The new index of the joint in the global list.</param>
    void UpdateJointIndex(Joint joint, int newIndex);


    public void AddConnectedJoint(ConnectedJointData jointData);
    public void RemoveConnectedJoint(Joint joint);
    public void ClearConnectedJoints();
}