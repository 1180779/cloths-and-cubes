using System.Diagnostics;

using Engine.Collision.ContactGraph;

namespace Engine;

public enum ContactResolverMode : uint
{
    // Checking each pair of contacts whether it's been affected by the one just resolved, O(n^2)
    LOOP,
    // Building the ContactGraph structure and utilising a PriorityQueue to handle contact severity
    CONTACT_GRAPH,
    // Millington's sorted list approach, simplified by using C# List<T> capabilities
    SORTED_LIST
}

public class ContactResolver
{
    // Change this to switch between different contact resolution strategies
    private const ContactResolverMode Mode = ContactResolverMode.LOOP;
    private const bool VERBAL = false;

    private uint velocityIterations;
    private uint positionIterations;
    private Real velocityEpsilon;
    private Real positionEpsilon;
    private bool validSettings = false;

    public Real PositionEpsilon => positionEpsilon;
    public Real VelocityEpsilon => velocityEpsilon;

    public uint VelocityIterationsUsed { get; set; } = 0;
    public uint PositionIterationsUsed { get; set; } = 0;

    public bool IsValid =>
        velocityIterations > 0 && positionIterations > 0 && positionEpsilon >= 0 && velocityEpsilon >= 0;

    public ContactResolver()
    {
    }

    public ContactResolver(uint iterations, Real velocityEpsilon = (Real)0.01, Real positionEpsilon = (Real)0.01)
    {
        positionIterations = iterations;
        velocityIterations = iterations;
        this.velocityEpsilon = velocityEpsilon;
        this.positionEpsilon = positionEpsilon;
    }

    public ContactResolver(
        uint positionIterations,
        uint velocityIterations,
        Real velocityEpsilon = (Real)0.01,
        Real positionEpsilon = (Real)0.01)
    {
        this.positionIterations = positionIterations;
        this.velocityIterations = velocityIterations;
        this.velocityEpsilon = velocityEpsilon;
        this.positionEpsilon = positionEpsilon;
    }

    public void ResolveContacts(Contact[] contacts, uint numContacts, Real duration)
    {
        if (numContacts == 0) return;
        if (!IsValid) return;
        PrepareContacts(contacts, numContacts, duration);
        AdjustPositions(contacts, numContacts, duration);
        AdjustVelocities(contacts, numContacts, duration);

        if(Mode == ContactResolverMode.SORTED_LIST)
            ClearInternals();
    }

    private void ClearInternals()
    {
        foreach(var contact in _orderedContactsPos)
        {
            offset = 0;
            contact.ContactQueueIdx = -1;
            contact.previousInOrder = null;
            contact.nextInOrder = null;
            contact.nextObject[0] = null;
            contact.nextObject[1] = null;
            contact.Body[0]!.Contacts.Clear();
            if (contact.Body[1] != null)
            {
                contact.Body[1]!.Contacts.Clear();
            }
        }

        _orderedContactsPos.Clear();
        _orderedContactsVel.Clear();
        _adjustedContactsPos.Clear();
        _adjustedContactsVel.Clear();
    }

    private List<Contact> _orderedContactsPos = new();
    private List<Contact> _adjustedContactsPos = new();
    private List<Contact> _orderedContactsVel = new();
    private List<Contact> _adjustedContactsVel = new();
    private int offset = 0;

    protected void PrepareContacts(Contact[] contacts, uint numContacts, Real duration)
    {
        for (var i = 0; i < numContacts; ++i)
        {
            // Calculate the internal contact data (inertia, basis, etc).
            contacts[i].CalculateInternals(duration);
        }
    }

    protected void PrepareContactsVel(Contact[] contacts, uint numContacts)
    {
        offset = 0;
        _orderedContactsVel.Clear();
        // Generate contact velocity and axis information.
        for (var i = 0; i < numContacts; ++i)
        {
            _orderedContactsVel.Add(contacts[i]);
        }
        _orderedContactsVel.Sort(delegate (Contact c1, Contact c2)
        {
            if (c1.DesiredDeltaVelocity > c2.DesiredDeltaVelocity) return -1;
            if (c1.DesiredDeltaVelocity < c2.DesiredDeltaVelocity) return 1;
            return 0;
        });

        for (int i = 0; i < _orderedContactsVel.Count; ++i) 
        {
            _orderedContactsVel[i].ContactQueueIdx = i;
            if (i > 0)
            {
                _orderedContactsVel[i].previousInOrder = _orderedContactsVel[i - 1];
            }
            if (i < _orderedContactsVel.Count - 1)
            {
                _orderedContactsVel[i].nextInOrder = _orderedContactsVel[i + 1];
            }
            _orderedContactsVel[i].Body[0]!.Contacts.Add(_orderedContactsVel[i]);
            if (_orderedContactsVel[i].Body[0]!.Contacts.Count > 0)
            {
                _orderedContactsVel[i].Body[0]!.Contacts[_orderedContactsVel[i].Body[0]!.Contacts.Count - 1].nextObject[0] = _orderedContactsVel[i];
            }
            if (_orderedContactsVel[i].Body[1] != null)
            {
                _orderedContactsVel[i].Body[1]!.Contacts.Add(_orderedContactsVel[i]);
                if (_orderedContactsVel[i].Body[1]!.Contacts.Count > 0)
                {
                    _orderedContactsVel[i].Body[1]!.Contacts[_orderedContactsVel[i].Body[1]!.Contacts.Count - 1].nextObject[1] = _orderedContactsVel[i];
                }
            }
        }
    }

    protected void moveToAdjusted(Contact contact)
    {
        if(contact.previousInOrder!= null)
            contact.previousInOrder.nextInOrder = contact.nextInOrder;
        if (contact.nextInOrder != null)
            contact.nextInOrder.previousInOrder = contact.previousInOrder;

        _adjustedContactsPos.Add(contact);
    }

    protected void moveToAdjustedVel(Contact contact)
    {
       if(contact.previousInOrder!=null)
            contact.previousInOrder.nextInOrder = contact.nextInOrder;
        if (contact.nextInOrder != null)
            contact.nextInOrder.previousInOrder = contact.previousInOrder;

        _adjustedContactsVel.Add(contact);
    }
    protected void moveToAdjusted(int idx)
    {
        _adjustedContactsPos.Add(_orderedContactsPos[idx - offset]);
        _orderedContactsPos.RemoveAt(idx-offset);
        offset++;
    }

    protected void moveToAdjustedVel(int idx)
    {
        _adjustedContactsVel.Add(_orderedContactsVel[idx - offset]);
        _orderedContactsVel.RemoveAt(idx - offset);
        offset++;
    }

 
    

    protected void AdjustVelocitiesList(Contact[] contacts, uint numContacts, Real duration)
    {
        PrepareContactsVel(contacts, numContacts);

        if (_orderedContactsVel.Count == 0) return;

        Vector3[] velocityChange = [new(), new()];
        Vector3[] rotationChange = [new(), new()];
        // iteratively handle impacts in order of severity.
        VelocityIterationsUsed = 0;
        while (VelocityIterationsUsed < velocityIterations)
        {
            var contact = _orderedContactsVel[0];
            var max = contact.DesiredDeltaVelocity;
            if (max < velocityEpsilon) break;


            contact.MatchAwakeState();
            contact.ApplyVelocityChange(velocityChange, rotationChange);
            Vector3 deltaVelOriginal = velocityChange[0] +
                    rotationChange[0].VectorProduct(
                        contact.RelativeContactPosition[0]);

            contact.ContactVelocity +=
                contact.ContactToWorld.TransformTranspose(deltaVelOriginal);
            var oldDelta = contact.DesiredDeltaVelocity;
            contact.CalculateDesiredDeltaVelocity(duration);

            if (contact.Body[1] != null)
            {
                deltaVelOriginal = velocityChange[1] +
                        rotationChange[1].VectorProduct(
                            contact.RelativeContactPosition[1]);
                contact.ContactVelocity -=
                    contact.ContactToWorld.TransformTranspose(deltaVelOriginal);
                contact.CalculateDesiredDeltaVelocity(duration);
            }

            var moved = new bool[_orderedContactsVel.Count];
            Array.Fill(moved, false);
            moveToAdjustedVel(0);
            moved[0] = true;

            Debug.Assert(contact.Body[0] != null);

            var contactsToMove = new List<Contact>();
            var bodyContacts = contact.Body[0]!.Contacts;
            foreach (var bodyContact in bodyContacts)
            {
                if (bodyContact == contact) continue;
                int d = 0;
                int b = (bodyContact.Body[0] == contact.Body[0]) ? 0 : 1;

                Vector3 deltaVel = velocityChange[d] +
                    rotationChange[d].VectorProduct(
                        bodyContact.RelativeContactPosition[b]);
                bodyContact.ContactVelocity +=
                    bodyContact.ContactToWorld.TransformTranspose(deltaVel)
                    * (b != 0 ? -1 : 1);
                var oldDelta2 = bodyContact.DesiredDeltaVelocity;
                bodyContact.CalculateDesiredDeltaVelocity(duration);
                if (!moved[bodyContact.ContactQueueIdx])
                {
                    moved[bodyContact.ContactQueueIdx] = true;
                    contactsToMove.Add(bodyContact); }
            }

            if (contact.Body[1] != null)
            {

                bodyContacts = contact.Body[1]!.Contacts;

                foreach (var bodyContact in bodyContacts)
                {
                    int d = 1;
                    int b = (bodyContact.Body[0] == contact.Body[1]) ? 0 : 1;
                    if (bodyContact == contact) continue;
                    Vector3 deltaVel = velocityChange[d] +
                        rotationChange[d].VectorProduct(
                            bodyContact.RelativeContactPosition[b]);
                    bodyContact.ContactVelocity +=
                        bodyContact.ContactToWorld.TransformTranspose(deltaVel)
                        * (b != 0 ? 1 : -1);
                    bodyContact.CalculateDesiredDeltaVelocity(duration);
                    if (!moved[bodyContact.ContactQueueIdx])
                    {
                        moved[bodyContact.ContactQueueIdx] = true;
                        contactsToMove.Add(bodyContact); }
                }
            }

            contactsToMove.Sort(delegate (Contact c1, Contact c2)
            {
                if (c1.ContactQueueIdx > c2.ContactQueueIdx) return 1;
                if (c1.ContactQueueIdx < c2.ContactQueueIdx) return -1;
                return 0;
            });


            foreach (var bodyContact in contactsToMove)
            {
                moveToAdjustedVel(bodyContact.ContactQueueIdx);
            }
            

            _adjustedContactsVel.Sort(delegate (Contact c1, Contact c2)
            {
                if (c1.DesiredDeltaVelocity > c2.DesiredDeltaVelocity) return -1;
                if (c1.DesiredDeltaVelocity < c2.DesiredDeltaVelocity) return 1;
                return 0;
            });
            int k = 0;
            for (int j = 0; j < _adjustedContactsVel.Count;)
            {
                if (_orderedContactsVel.Count == 0 || _orderedContactsVel.Count == k || _adjustedContactsVel[j].DesiredDeltaVelocity > _orderedContactsVel[k].DesiredDeltaVelocity)
                {
                    _orderedContactsVel.Insert(k, _adjustedContactsVel[j]);
                    j++;
                }
                else
                {
                    k++;
                }
            }
            for (int j = 0; j < _orderedContactsVel.Count; j++)
            {
                _orderedContactsVel[j].ContactQueueIdx = j;
            }
            _adjustedContactsVel.Clear();
            VelocityIterationsUsed++;
            offset = 0;
        }
    }


    protected void AdjustVelocitiesGraph(Contact[] contacts, uint numContacts, Real duration)
    {
        ContactGraph graph = ContactGraph.Build(contacts, numContacts);
        graph.ResolveVelocities(velocityIterations, velocityEpsilon, duration);
    }

    protected void AdjustVelocitiesLoop(Contact[] contacts, uint numContacts, Real duration)
    {
        Vector3[] velocityChange = [new(), new()];
        Vector3[] rotationChange = [new(), new()];
        while (VelocityIterationsUsed < velocityIterations)
        {
            //Console.WriteLine($"ROUND {VelocityIterationsUsed+1}");
            // Find contact with maximum magnitude of probable velocity change.
            var max = velocityEpsilon;
            var index = numContacts;
            for (uint i = 0; i < numContacts; i++)
            {
                if (contacts[i].DesiredDeltaVelocity > max)
                {
                    max = contacts[i].DesiredDeltaVelocity;
                    index = i;
                }
            }

            if (index == numContacts) break;

            // Match the awake state at the contact
            contacts[index].MatchAwakeState();

            // Do the resolution on the contact that came out top.
            contacts[index].ApplyVelocityChange(velocityChange, rotationChange);

            // With the change in velocity of the two bodies, the update of
            // contact velocities means that some of the relative closing
            // velocities need recomputing.
            for (uint i = 0; i < numContacts; i++)
            {
                // Check each body in the contact
                for (uint b = 0; b < 2; b++)
                    if (contacts[i].Body[b] != null)
                    {
                        // Check for a match with each body in the newly
                        // resolved contact
                        for (uint d = 0; d < 2; d++)
                        {
                            if (contacts[i].Body[b] == contacts[index].Body[d])
                            {
                                Vector3 deltaVel = velocityChange[d] +
                                    rotationChange[d].VectorProduct(
                                        contacts[i].RelativeContactPosition[b]);

                                // The sign of the change is negative if we're dealing
                                // with the second body in a contact.
                                contacts[i].ContactVelocity +=
                                    contacts[i].ContactToWorld.TransformTranspose(deltaVel)
                                    * (b != 0 ? -1 : 1);
                                var oldDelta = contacts[i].DesiredDeltaVelocity;
                                contacts[i].CalculateDesiredDeltaVelocity(duration);
                                //Console.WriteLine($"\t[{contacts[i].ContactPoint.X:0.##}, {contacts[i].ContactPoint.Z:0.##}]: ({oldDelta:0.##}) -> ({contacts[i].DesiredDeltaVelocity:0.##})");
                                //Console.WriteLine($"\t\tVelocity: {contacts[i].ContactVelocity}, Delta Velocity: {deltaVel}");
                            }
                        }
                    }
            }

            VelocityIterationsUsed++;
        }
    }

    protected void AdjustVelocities(Contact[] contacts, uint numContacts, Real duration)
    {
        // iteratively handle impacts in order of severity.
        VelocityIterationsUsed = 0;

        switch (Mode)
        {
            case ContactResolverMode.LOOP:
                AdjustVelocitiesLoop(contacts, numContacts, duration);
                break;
            case ContactResolverMode.CONTACT_GRAPH:
                AdjustVelocitiesGraph(contacts, numContacts, duration);
                break;
            case ContactResolverMode.SORTED_LIST:
                AdjustVelocitiesList(contacts, numContacts, duration);
                break;
            default:
                AdjustVelocitiesLoop(contacts, numContacts, duration);
                break;
        }
    }

    protected void AdjustPositionsList(Contact[] contacts, uint numContacts, Real duration)
    {
        PrepareContactsPos(contacts, numContacts);

        if (_orderedContactsPos.Count == 0) return;

        Vector3[] linearChange = [new(), new()];
        Vector3[] angularChange = [new(), new()];


        for (uint i = 0; i < positionIterations; ++i)
        {
            var contact = _orderedContactsPos[0];
            var max = contact.Penetration;
            if (max < positionEpsilon) break;


            contact.MatchAwakeState();
            contact.ApplyPositionChange(linearChange, angularChange, max);

            Vector3 deltaPosOriginal = linearChange[0] +
                    angularChange[0].VectorProduct(
                        contact.RelativeContactPosition[0]);
            contact.Penetration -=
                deltaPosOriginal.ScalarProduct(contact.ContactNormal);

            if (contact.Body[1] != null)
            {
                deltaPosOriginal = linearChange[1] +
                        angularChange[1].VectorProduct(
                            contact.RelativeContactPosition[1]);
                contact.Penetration +=
                    deltaPosOriginal.ScalarProduct(contact.ContactNormal);
            }

            var moved = new bool[_orderedContactsPos.Count];
            Array.Fill(moved, false);
            moveToAdjusted(0);
            moved[0] = true;
            var contactsToMove = new List<Contact>();

            Debug.Assert(contact.Body[0] != null);
            //Contact? bodyContact = contact.Body[0]!.Contacts[0];
            var bodyContacts = contact.Body[0]!.Contacts;
            foreach (var bodyContact in bodyContacts)
            {
                if (bodyContact == contact) continue;
                Vector3 deltaPosition = linearChange[0] +
                    angularChange[0].VectorProduct(
                        bodyContact.RelativeContactPosition[0]);
                bodyContact.Penetration -=
                    deltaPosition.ScalarProduct(bodyContact.ContactNormal);

                if (!moved[bodyContact.ContactQueueIdx])
                {
                    moved[bodyContact.ContactQueueIdx] = true;
                    contactsToMove.Add(bodyContact);
                }
            }

            if (contact.Body[1] != null)
            {
                bodyContacts = contact.Body[1]!.Contacts;
                foreach (var bodyContact in bodyContacts)
                {
                    if (bodyContact == contact) continue;
                    Vector3 deltaPosition = linearChange[1] +
                        angularChange[1].VectorProduct(
                            bodyContact.RelativeContactPosition[1]);
                    bodyContact.Penetration +=
                        deltaPosition.ScalarProduct(bodyContact.ContactNormal);
                    if (!moved[bodyContact.ContactQueueIdx])
                    {
                        moved[bodyContact.ContactQueueIdx] = true;
                        contactsToMove.Add(bodyContact);
                    }
                }
            }




            contactsToMove.Sort(delegate (Contact c1, Contact c2)
            {
                if (c1.ContactQueueIdx > c2.ContactQueueIdx) return 1;
                if (c1.ContactQueueIdx < c2.ContactQueueIdx) return -1;
                return 0;
            });

            foreach (var bodyContact in contactsToMove)
            {
                moveToAdjusted(bodyContact.ContactQueueIdx);
            }


            _adjustedContactsPos.Sort(delegate (Contact c1, Contact c2)
            {
                if (c1.Penetration > c2.Penetration) return -1;
                if (c1.Penetration < c2.Penetration) return 1;
                return 0;
            });

            int k = 0;
            for (int j = 0; j < _adjustedContactsPos.Count;)
            {
                if (_orderedContactsPos.Count == 0 || _orderedContactsPos.Count == k || _adjustedContactsPos[j].Penetration > _orderedContactsPos[k].Penetration)
                {
                    //_adjustedContactsPos[j].ContactQueueIdx = k + j;
                    _orderedContactsPos.Insert(k, _adjustedContactsPos[j]);
                    j++;
                }
                else
                {
                    //_orderedContactsPos[k].ContactQueueIdx = k + j;
                    k++;
                }
            }
            for (int j = 0; j < _orderedContactsPos.Count; j++)
            {
                _orderedContactsPos[j].ContactQueueIdx = j;
            }
            _adjustedContactsPos.Clear();
            offset = 0;
        }
    }

    protected void PrepareContactsPos(Contact[] contacts, uint numContacts)
    {
        _orderedContactsPos.Clear();
        for(int i = 0; i < numContacts; i++)
        {
            _orderedContactsPos.Add(contacts[i]);
        }

        _orderedContactsPos.Sort(delegate (Contact c1, Contact c2)
        {
            if (c1.Penetration > c2.Penetration) return -1;
            if (c1.Penetration < c2.Penetration) return 1;
            return 0;
        });
        for (int i = 0; i < _orderedContactsPos.Count; i++)
        {
            _orderedContactsPos[i].ContactQueueIdx = i;
            if (i > 0)
            {
                _orderedContactsPos[i].previousInOrder = _orderedContactsPos[i - 1];
            }
            if (i < _orderedContactsVel.Count - 1)
            {
                _orderedContactsPos[i].nextInOrder = _orderedContactsPos[i + 1];
            }
            _orderedContactsPos[i].Body[0]!.Contacts.Add(_orderedContactsPos[i]);
            if (_orderedContactsPos[i].Body[0]!.Contacts.Count > 0)
            {
                _orderedContactsPos[i].Body[0]!.Contacts[_orderedContactsPos[i].Body[0]!.Contacts.Count - 1].nextObject[0] = _orderedContactsPos[i];
            }
            if (_orderedContactsPos[i].Body[1] != null)
            {
                _orderedContactsPos[i].Body[1]!.Contacts.Add(_orderedContactsPos[i]);
                if (_orderedContactsPos[i].Body[1]!.Contacts.Count > 0)
                {
                    _orderedContactsPos[i].Body[1]!.Contacts[_orderedContactsPos[i].Body[1]!.Contacts.Count - 1].nextObject[1] = _orderedContactsPos[i];
                }
            }
        }
    }

    protected void AdjustPositionsLoop(Contact[] contacts, uint numContacts, Real duration)
    {
        uint i = 0;
        uint index = 0;
        Vector3[] linearChange = [new(), new()];
        Vector3[] angularChange = [new(), new()];
        Real max = 0;
        Vector3 deltaPosition = new();
        while (PositionIterationsUsed < positionIterations)
        {
            // Find biggest penetration
            max = positionEpsilon;
            index = numContacts;
            for (i = 0; i < numContacts; i++)
            {
                if (contacts[i].Penetration > max)
                {
                    max = contacts[i].Penetration;
                    index = i;
                }
            }

            if (index == numContacts) break;

            if (VERBAL)
                Console.WriteLine($"ROUND {PositionIterationsUsed + 1}");

            // Match the awake state at the contact
            contacts[index].MatchAwakeState();

            // Resolve the penetration.
            contacts[index].ApplyPositionChange(
                linearChange,
                angularChange,
                max);

            // Again this action may have changed the penetration of other
            // bodies, so we update contacts.
            for (i = 0; i < numContacts; i++)
            {
                // Check each body in the contact
                for (uint b = 0; b < 2; b++)
                    if (contacts[i].Body[b] != null)
                    {
                        // Check for a match with each body in the newly
                        // resolved contact
                        for (uint d = 0; d < 2; d++)
                        {
                            if (contacts[i].Body[b] == contacts[index].Body[d])
                            {
                                deltaPosition = linearChange[d] +
                                    angularChange[d].VectorProduct(
                                        contacts[i].RelativeContactPosition[b]);

                                var oldPen = contacts[i].Penetration;
                                // The sign of the change is positive if we're
                                // dealing with the second body in a contact
                                // and negative otherwise (because we're
                                // subtracting the resolution)..
                                contacts[i].Penetration +=
                                    deltaPosition.ScalarProduct(contacts[i].ContactNormal)
                                    * (b != 0 ? 1 : -1);

                                if (VERBAL)
                                    Console.WriteLine($"\t[{contacts[i].ContactPoint.X:0.##}, {contacts[i].ContactPoint.Z:0.##}]: ({oldPen:0.####}) -> ({contacts[i].Penetration:0.####})");
                                //Console.WriteLine($"\t\tDelta Position: {deltaPosition}");
                            }
                        }
                    }
            }

            PositionIterationsUsed++;
        }
    }
    protected void AdjustPositions(Contact[] contacts, uint numContacts, Real duration)
    {
        // iteratively resolve interpenetrations in order of severity.
        PositionIterationsUsed = 0;
        switch (Mode)
        {
            case ContactResolverMode.LOOP:
                AdjustPositionsLoop(contacts, numContacts, duration);
                break;
            case ContactResolverMode.CONTACT_GRAPH:
                ContactGraph graph = ContactGraph.Build(contacts, numContacts);
                graph.ResolvePositions(positionIterations, positionEpsilon);
                break;
            case ContactResolverMode.SORTED_LIST:
                AdjustPositionsList(contacts, numContacts, duration);
                break;
            default:
                AdjustPositionsLoop(contacts, numContacts, duration);
                break;
        }
    }
}