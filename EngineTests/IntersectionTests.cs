using Engine;
using Engine.Collision;

namespace EngineTests;

public class IntersectionTests
{
    private static CollisionBox CreateBox(Vector3 center, Vector3 halfSize)
    {
        var box = new CollisionBox
        {
            HalfSize = halfSize,
            Body =
            {
                // Position the box body at the given center
                Position = center
            }
        };
        // Ensure transform is computed when needed
        box.Body.CalculateDerivedData();
        return box;
    }

    private static CollisionPlane CreatePlane(Vector3 normal, float offset)
    {
        // normal should be normalized for most collision code
        normal.Normalize();
        return new CollisionPlane
        {
            Direction = normal,
            Offset = offset
        };
    }

    private static CollisionData CreateCollisionData(
        uint maxContacts = 16,
        float friction = 0.5f,
        float restitution = 0.1f,
        float tolerance = 0.01f)
    {
        var data = new CollisionData
        {
            Friction = friction,
            Restitution = restitution,
            Tolerance = tolerance
        };
        data.Reset(maxContacts);
        return data;
    }

    [Test]
    public void BoxAbovePlane_NoCollision_ReturnsZeroContacts()
    {
        // Plane: y = 0 (normal up)
        var plane = CreatePlane(new Vector3(0, 1, 0), 0f);
        // Box centered at y=2, half-size 0.5 => bottom at y=1.5
        var box = CreateBox(new Vector3(0, 2, 0), new Vector3(0.5f, 0.5f, 0.5f));

        var data = CreateCollisionData(16);

        var written = CollisionDetector.BoxAndHalfSpace(box, plane, data);
        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(0), "No contacts should be generated when box is clearly above the plane.");
            Assert.That(data.ContactCount, Is.EqualTo(0));
            Assert.That(data.ContactsLeft, Is.EqualTo(16));
        });
    }

    [Test]
    public void BoxIntersectingPlane_GeneratesAtLeastOneContact()
    {
        // Plane: y = 0
        var plane = CreatePlane(new Vector3(0, 1, 0), 0f);
        // Box center y=0.25 with half-size 0.5 => bottom at y=-0.25 (intersects plane)
        var box = CreateBox(new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.5f, 0.5f));

        var data = CreateCollisionData(16);

        var written = CollisionDetector.BoxAndHalfSpace(box, plane, data);
        Assert.Multiple(() =>
        {
            Assert.That(written, Is.GreaterThan(0u), "At least one contact expected when box intersects plane.");
            Assert.That(data.ContactCount, Is.EqualTo(written));
            Assert.That(data.ContactsLeft, Is.EqualTo(16 - (int)written));
        });
    }

    [Test]
    public void BoxFullyBelowPlane_GeneratesContacts()
    {
        // Plane: y = 0
        var plane = CreatePlane(new Vector3(0, 1, 0), 0f);
        // Box center y=-2 with half-size 0.5 => fully below plane
        var box = CreateBox(new Vector3(0, -2f, 0), new Vector3(0.5f, 0.5f, 0.5f));

        var data = CreateCollisionData(16);

        uint written = CollisionDetector.BoxAndHalfSpace(box, plane, data);
        Assert.Multiple(() =>
        {
            Assert.That(written, Is.GreaterThan(0u), "Contacts are expected when the box lies fully behind the plane.");
            Assert.That(data.ContactCount, Is.EqualTo(written));
            Assert.That(data.ContactsLeft, Is.EqualTo(16 - (int)written));
        });
    }

    [Test]
    public void ContactBudget_IsRespected_WhenLimitedToOne()
    {
        // Plane: y = 0
        var plane = CreatePlane(new Vector3(0, 1, 0), 0f);
        // Intersecting configuration
        var box = CreateBox(new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.5f, 0.5f));

        var data = CreateCollisionData(1);

        uint written = CollisionDetector.BoxAndHalfSpace(box, plane, data);
        Assert.Multiple(() =>
        {
            Assert.That(written, Is.LessThanOrEqualTo(1u));
            Assert.That(data.ContactCount, Is.EqualTo(written));
            Assert.That(data.ContactsLeft, Is.EqualTo(1 - (int)written),
                "ContactsLeft should be reduced by the number actually written.");
        });
    }

    [Test]
    public void NoContactsWritten_WhenNoBudgetLeft()
    {
        // Plane: y = 0
        var plane = CreatePlane(new Vector3(0, 1, 0), 0f);
        // Intersecting configuration
        var box = CreateBox(new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.5f, 0.5f));

        var data = CreateCollisionData(0);

        uint written = CollisionDetector.BoxAndHalfSpace(box, plane, data);
        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(0u), "No contacts should be written if there is no budget.");
            Assert.That(data.ContactCount, Is.EqualTo(0));
            Assert.That(data.ContactsLeft, Is.EqualTo(0));
        });
    }
}