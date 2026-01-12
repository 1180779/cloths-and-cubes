using System.Runtime.CompilerServices;

using Engine.Collision;

namespace Engine.ContactGenerators;

// TODO: change to other data structure if needed
/// <summary>
/// The global list of joints in the scene. 
/// If a joint is created between two bodies, it should be added to this list.
/// If a joint is removed, it should be removed from this list as well
/// (ex. when one of the connecting bodies is removed from the scene).
/// </summary>
/// <note>
/// <see cref="ConnectedJointData"/> can be used to track connected joints within bodies.
/// </note>
public struct GlobalJointsList()
{
    public List<Joint> Joints { get; } = new();

    /// <summary>
    /// Check if the specified joint exists in the global list.
    ///
    /// This is done by searching for the joint reference.
    /// Prefer keeping track of adding and removing of joints to avoid searches if possible.
    /// </summary>
    /// <param name="joint"></param>
    /// <returns>If the joint is found, it returns true; otherwise, it returns false.</returns>
    public bool JointExists(Joint joint)
    {
        return Joints.Contains(joint);
    }

    /// <summary>
    /// Add the specified joint to the global list.
    /// </summary>
    /// <param name="joint">The joint to be added.</param>
    /// <returns>The index at which the joint was added within the global list.</returns>
    public int AddJoint(Joint joint)
    {
        Joints.Add(joint);
        return Joints.Count - 1;
    }

    // TODO: change to scheduled removal to avoid issues during iteration

    /// <summary>
    /// Generates contacts for all joints in the global list and adds them to the provided CollisionData.
    /// </summary>
    /// <param name="collisionData"></param>
    public void GenerateContactsFromJoints(CollisionData collisionData)
    {
        foreach (var joint in Joints)
        {
            if (!collisionData.HasMoreContacts())
                return;
            joint.AddContacts(collisionData);
        }
    }

    /// <summary>
    /// Remove all joints from the global list and from their associated trackables.
    /// </summary>
    public void Clear()
    {
        foreach (var joint in Joints)
        {
            joint.RemoveFromTrackables();
        }

        Joints.Clear();
    }

    /// <summary>
    /// Remove the specified joint from the global list.
    /// <param name="joint">The joint to be removed.</param>
    /// </summary>
    /// <note>
    /// This is done by searching for the joint reference and removing it if found. Prefer using
    /// the <see cref="RemoveJoint(ConnectedJointData)"/> overload that takes ConnectedJointData
    /// if the index is known for efficiency.
    /// </note>
    public void RemoveJoint(Joint joint)
    {
        try
        {
            var index = Joints.FindIndex(0, j => j == joint);
            SwapAndPop(index);
        }
        catch (ArgumentOutOfRangeException) { } // Ignore if not found
    }

    /// <summary>
    /// Remove the specified joint from the global list.
    /// <param name="jointData">The data of the joint to be removed.</param>
    /// </summary>
    public void RemoveJoint(ConnectedJointData jointData)
    {
        if (jointData.Index < 0 || jointData.Index >= Joints.Count)
            // Ignore invalid index
            return;

        // Remove the joint from the list by doing swap and pop for efficiency
        SwapAndPop(jointData.Index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SwapAndPop(int index)
    {
        int lastIndex = Joints.Count - 1;

        // If we're removing the last element, no swap is needed
        if (index == lastIndex)
        {
            Joints.RemoveAt(lastIndex);
            return;
        }

        Joint movedJoint = Joints[lastIndex];
        Joints[index] = movedJoint;
        Joints.RemoveAt(lastIndex);

        movedJoint.UpdateIndicesInTrackables(index);
    }
}