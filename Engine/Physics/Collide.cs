namespace Engine.Physics
{
    struct BoundingBox
    {
        Vector3 center;
        Vector3 halfSize;
    };
    /**
    * Stores a potential contact to check later.
*/
    struct PotentialContact
    {
        /**
        * Holds the bodies that might be in contact.
        Ensure body has only 0 and 1 in the tab
*/
        RigidBody[] body;
    };
    
}