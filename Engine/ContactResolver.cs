using Engine.Collision.ContactGraph;

namespace Engine;

public class ContactResolver
{
    private uint velocityIterations;
    private uint positionIterations;
    private Real velocityEpsilon;
    private Real positionEpsilon;
    private bool validSettings = false;

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
    }

    protected void PrepareContacts(Contact[] contacts, uint numContacts, Real duration)
    {
        // Generate contact velocity and axis information.
        for (var i = 0; i < numContacts; ++i)
        {
            // Calculate the internal contact data (inertia, basis, etc).
            contacts[i].CalculateInternals(duration);
        }
    }

    protected void AdjustVelocities(Contact[] contacts, uint numContacts, Real duration)
    {
        Vector3[] velocityChange = [new(), new()];
        Vector3[] rotationChange = [new(), new()];

        // iteratively handle impacts in order of severity.
        VelocityIterationsUsed = 0;
        while (VelocityIterationsUsed < velocityIterations)
        {
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
                                contacts[i].CalculateDesiredDeltaVelocity(duration);
                            }
                        }
                    }
            }

            VelocityIterationsUsed++;
        }
    }

    protected void AdjustPositions(Contact[] contacts, uint numContacts, Real duration)
    {
        uint i = 0;
        uint index = 0;
        Vector3[] linearChange = [new(), new()];
        Vector3[] angularChange = [new(), new()];
        Real max = 0;
        Vector3 deltaPosition = new();

        // iteratively resolve interpenetrations in order of severity.
        PositionIterationsUsed = 0;

        ContactGraph graph = ContactGraph.Build(contacts);
        graph.ResolveGraph(positionIterations, positionEpsilon);
        return;

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

                                // The sign of the change is positive if we're
                                // dealing with the second body in a contact
                                // and negative otherwise (because we're
                                // subtracting the resolution)..
                                contacts[i].Penetration +=
                                    deltaPosition.ScalarProduct(contacts[i].ContactNormal)
                                    * (b != 0 ? 1 : -1);
                            }
                        }
                    }
            }

            PositionIterationsUsed++;
        }
    }
}