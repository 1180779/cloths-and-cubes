namespace Engine.ContactGenerators;

/// <summary>
/// A rigid body keeping track of a single joint attached to it.
/// </summary>
public interface IBodyWithSingleJoint : IJointTrackable
{
    /// <summary>
    /// The connected joint data for this RigidBody. 
    /// </summary>
    public ConnectedJointData ConnectedJoint { get; set; }

    void IJointTrackable.AddConnectedJoint(ConnectedJointData jointData)
    {
        if (!ConnectedJoint.IsSet)
            ConnectedJoint = jointData;
    }

    void IJointTrackable.RemoveConnectedJoint(Joint joint)
    {
        if (ConnectedJoint.Joint == joint)
        {
            // Clear will not work, as ConnectedJoint is a struct property. 
            // ConnectedJoint.Clear();
            ConnectedJoint = new ConnectedJointData();
        }
    }

    void IJointTrackable.UpdateJointIndex(Joint joint, int newIndex)
    {
        if (ConnectedJoint.Joint == joint)
        {
            var jointData = ConnectedJoint;
            jointData.SetIndex(newIndex);
            ConnectedJoint = jointData;
        }
    }
}