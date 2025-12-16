namespace Engine.Collision;

public class CollisionData
{
    public CollisionData()
    {
    }

    /// <summary>
    /// Holds all contacts. Acts like a box with empty contents for filling. 
    /// (Acts like 'contactArray' in C++ code)
    /// </summary>
    public Contact[] ContactList = [];

    /// <summary>
    /// Index of the next free contact slot
    /// (Acts like the 'contacts' pointer in C++ code)
    /// </summary>
    public int NextContactIndex;

    /// <summary>
    /// The number of empty contacts left in the array
    /// </summary>
    public int ContactsLeft;

    /// <summary>
    /// Number of contacts found so far.
    /// </summary>
    public uint ContactCount;

    /// <summary>
    /// Friction value to write into any collisions.
    /// </summary>
    public Real Friction;

    /// <summary>
    /// Restitution value to write into any collisions.
    /// </summary>
    public Real Restitution;

    /// <summary>
    /// Collision tolerance — even uncolliding objects this close should have collisions generated.
    /// </summary>
    public Real Tolerance;

    /// <summary>
    /// Checks if there are more contacts available.
    /// </summary>
    public bool HasMoreContacts()
    {
        return ContactsLeft > 0;
    }

    /// <summary>
    /// Resets the data so that it has no used contacts recorded.
    /// </summary>
    public void Reset(uint maxContacts)
    {
        for (int i = 0; i < ContactCount; i++)
        {
            ContactList[i] = new Contact();
        }

        ContactsLeft = (int)maxContacts;
        ContactCount = 0;
        NextContactIndex = 0;

        if (ContactList.Length < (int)maxContacts)
        {
            /* fill the difference with empty contacts for use */
            int oldSize = ContactList.Length;
            Array.Resize(ref ContactList, (int)maxContacts);
            for (int i = oldSize; i < ContactList.Length; i++)
            {
                ContactList[i] = new Contact();
            }
        }
    }

    /// <summary>
    /// Notifies the data that the given number of contacts has been added.
    /// </summary>
    public void AddContacts(uint count)
    {
        // Reduce the number of contacts remaining, add the number used
        ContactsLeft -= (int)count;
        ContactCount += count;
    }
}