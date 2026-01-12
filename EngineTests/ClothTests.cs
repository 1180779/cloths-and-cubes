using Engine;
using Engine.Force;
using Engine.RigidBodies;

namespace EngineTests;

[TestFixture]
public class ClothTests
{
    [Test]
    [TestCaseSource(nameof(CylinderIntersectionTestCases))]
    public void TestClothParticleRegeneration_PreservingCenter_CornersAreCorrect(
        Cloth cloth,
        int newSizeX,
        int newSizeY,
        float newSpringLength,
        float newSpringConstant,
        float newParticleMass)
    {
        // Arrange
        var originalCenter = cloth.Center;

        // Act
        cloth.RegenerateGridPreservingTheCenter(newSizeX, newSizeY, newSpringLength, newSpringConstant,
            newParticleMass);

        // Assert
        // Check that the center is preserved
        var newCenter = cloth.Center;
        Assert.That(newCenter.X, Is.EqualTo(originalCenter.X).Within(0.001f));
        Assert.That(newCenter.Y, Is.EqualTo(originalCenter.Y).Within(0.001f));
        Assert.That(newCenter.Z, Is.EqualTo(originalCenter.Z).Within(0.001f));

        // Check corners
        Assert.IsInstanceOf<ClothRigidParticleInCorner>(cloth.Particles[0, 0]);
        Assert.IsInstanceOf<ClothRigidParticleInCorner>(cloth.Particles[0, newSizeY - 1]);
        Assert.IsInstanceOf<ClothRigidParticleInCorner>(cloth.Particles[newSizeX - 1, 0]);
        Assert.IsInstanceOf<ClothRigidParticleInCorner>(cloth.Particles[newSizeX - 1, newSizeY - 1]);

        // Check the total number of corner particles
        int cornerCount = 0;
        for (int i = 0; i < newSizeX; i++)
        {
            for (int j = 0; j < newSizeY; j++)
            {
                if (cloth.Particles[i, j] is ClothRigidParticleInCorner)
                {
                    cornerCount++;
                }
            }
        }

        // Check indices for each particle
        for (int i = 0; i < newSizeX; i++)
        {
            for (int j = 0; j < newSizeY; j++)
            {
                var particle = cloth.Particles[i, j];
                Assert.That(particle.XIndex, Is.EqualTo(i), $"Particle at ({i},{j}) has incorrect X index");
                Assert.That(particle.YIndex, Is.EqualTo(j), $"Particle at ({i},{j}) has incorrect Y index");
            }
        }

        Assert.That(cornerCount, Is.EqualTo(4),
            "There should be exactly 4 corner particles after center-preserving regeneration");
    }

    private static IEnumerable<TestCaseData> CylinderIntersectionTestCases()
    {
        yield return new TestCaseData(
            new Cloth(
                new ForceRegistry(), 4, 4,
                1.0f, 100.0f, 1.0f
            ),
            6, 6,
            1.0f, 100.0f, 1.0f
        );

        yield return new TestCaseData(
            new Cloth(
                new ForceRegistry(), 5, 5,
                1.0f, 100.0f, 1.0f
            ),
            3, 3,
            1.0f, 100.0f, 1.0f
        );

        yield return new TestCaseData(
            new Cloth(
                new ForceRegistry(), 6, 4,
                1.0f, 100.0f, 1.0f
            ),
            8, 5,
            1.0f, 100.0f, 1.0f
        );
    }
}