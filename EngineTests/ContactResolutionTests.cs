using Engine;
using Engine.Collision;

namespace EngineTests;

public class ContactResolutionTests
{
    const float PositionEpsilon = 0.01f;

    private static CollisionBox CreateBox(Vector3 center, Vector3 halfSize, Quaternion? orientation = null)
    {
        var box = new CollisionBox
        {
            HalfSize = halfSize,
            Body = { Position = center, Orientation = orientation ?? new Quaternion(), InverseMass = 1.0f, }
        };
        box.Body.CalculateDerivedData();
        return box;
    }

    private static CollisionPlane CreatePlane(Vector3 normal, float offset)
    {
        normal.Normalize();
        return new CollisionPlane { Direction = normal, Offset = offset };
    }

    private static CollisionData CreateCollisionData(
        uint maxContacts = 16,
        float friction = 0.5f,
        float restitution = 0.1f,
        float tolerance = 0.01f)
    {
        var data = new CollisionData { Friction = friction, Restitution = restitution, Tolerance = tolerance };
        data.Reset(maxContacts);
        return data;
    }

    private static IEnumerable<TestCaseData> ContactResolutionTestCases()
    {
        yield return new TestCaseData(
            new Vector3(0, 0.4f, 0), new Vector3(0.5f, 0.5f, 0.5f), new Quaternion(),
            new Vector3(0, 1, 0), 0f,
            4u,
            "Face-on penetration"
        ).SetName("AdjustsPositions_ToResolvePenetration");

        // The following test case is set up to balance the cube on a single vertex.
        // The rotation axis is perpendicular to the cube's diagonal and the world's Y-axis.
        // The angle is calculated accordingly
        const float angleToBalanceDeg = 54.7356f;

        var rotationToBalance =
            Quaternion.FromAxisAngle(Vector3.CrossProduct(new Vector3(1, 1, 1), new Vector3(0, 1, 0)),
                Vector3.ScalarProduct(new Vector3(1, 1, 1).Normalise(), new Vector3(0, 1, 0).Normalise()) * 180 /
                MathF.PI);

        yield return new TestCaseData(
            new Vector3(0, MathF.Sqrt(3) - 0.1f, 0), new Vector3(1, 1, 1), rotationToBalance,
            new Vector3(0, 1, 0), 0f,
            1u,
            "Single vertex contact"
        ).SetName("SingleVertexContact_ResolvesCorrectly");

        yield return new TestCaseData(
            new Vector3(0, 0.5f, 0), new Vector3(1, 1, 1), Quaternion.FromAxisAngle(new Vector3(0, 0, 1), 45.0f),
            new Vector3(0, 1, 0), 0f,
            2u,
            "Two vertex contact"
        ).SetName("TwoVertexContact_ResolvesCorrectly");
    }

    [Test, TestCaseSource(nameof(ContactResolutionTestCases))]
    public void ContactResolver_ResolvesPenetrationCorrectly(
        Vector3 boxCenter,
        Vector3 boxHalfSize,
        Quaternion boxOrientation,
        Vector3 planeNormal,
        float planeOffset,
        uint expectedContacts,
        string description)
    {
        var plane = CreatePlane(planeNormal, planeOffset);
        var box = CreateBox(boxCenter, boxHalfSize, boxOrientation);

        var data = CreateCollisionData(16, tolerance: PositionEpsilon);
        var written = CollisionDetector.BoxAndHalfSpace(box, plane, data);
        Assert.That(written, Is.EqualTo(expectedContacts),
            $"Initial contact count for '{description}' should be {expectedContacts}.");

        // Contact points should lay on the plane
        for (int i = 0; i < written; ++i)
        {
            Assert.That(data.ContactList[i].ContactPoint.Y, Is.EqualTo(0.0f).Within(Core.Epsilon),
                $"Contact point {i} Y coordinate should be on the plane for '{description}'.");
        }

        if (written == 0)
        {
            Assert.Pass("No contacts generated initially, nothing to resolve.");
            return;
        }

        float initialPenetration = 0;
        for (int i = 0; i < written; i++)
        {
            if (data.ContactList[i].Penetration > initialPenetration)
                initialPenetration = data.ContactList[i].Penetration;
        }

        Assert.That(initialPenetration, Is.GreaterThan(0.0f), "Initial penetration should be positive.");

        var resolver = new ContactResolver(10, positionEpsilon: PositionEpsilon);
        resolver.ResolveContacts(data.ContactList, data.ContactCount, 0.016f);

        var resolvedData = CreateCollisionData(16);
        var resolvedWritten = CollisionDetector.BoxAndHalfSpace(box, plane, resolvedData);

        if (resolvedWritten > 0)
        {
            float maxPenetration = 0;
            for (int i = 0; i < resolvedWritten; i++)
            {
                if (resolvedData.ContactList[i].Penetration > maxPenetration)
                    maxPenetration = resolvedData.ContactList[i].Penetration;
            }

            Assert.That(maxPenetration, Is.LessThanOrEqualTo(PositionEpsilon),
                $"Penetration not resolved for '{description}'.");
        }
        else
        {
            Assert.That(resolvedWritten, Is.EqualTo(0u), $"Contacts should be resolved for '{description}'.");
        }
    }
}