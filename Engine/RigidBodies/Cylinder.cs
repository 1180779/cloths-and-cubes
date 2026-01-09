using System.Runtime.CompilerServices;

using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;

namespace Engine.RigidBodies;

public sealed class Cylinder : CollisionCylinder, IBoxable
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static BoundingBox GetGetBoundingBox(CollisionPrimitive primitive, Real radius, Real height)
    {
        //
        // for explanation see
        // https://iquilezles.org/articles/diskbbox/
        //
        // visualization of ellipse bounding box here
        // https://www.shadertoy.com/view/Xtjczw
        //
        
        primitive.Body.CalculateDerivedData();
        primitive.CalculateInternals();

        Matrix3 rotation = primitive.Transform.Matrix3;
        float h2 = height / 2f;
        
        // For a cylinder oriented along the Z-axis:
        // - The circular cross-section has radius R in the XY plane
        // - The height extends ±h2 along the Z axis
        
        // Calculate the contribution of the cylinder's height along each world axis
        // This is just |rotation · (0, 0, h2)| = |rotation_axisZ| · h2
        Vector3 halfHeightContribution = rotation.Axis(2).Abs() * h2;
        
        // Calculate the contribution of the circular cross-section to each world axis. 
        // The circular cross-section projects onto each world axis as an ellipse. 
        //
        // For an ellipse we get P_axis = C_axis + sqrt( u_axis² + v_axis² )
        // 
        // where u and v are vectors that define the ellipse (in this case circle) in the 3d space 
        // (along with the center point)
        // (P = c + u·sin(omega) + v·cos(omega))
        // 
        //
        // This maximum extent needs to be calculated for each of the axes. 
        //
        Vector3 radiusContribution = new(
            MathF.Sqrt(
                rotation.Data[0] * rotation.Data[0] + 
                rotation.Data[1] * rotation.Data[1]) * radius,
            MathF.Sqrt(
                rotation.Data[3] * rotation.Data[3] + 
                rotation.Data[4] * rotation.Data[4]) * radius,
            MathF.Sqrt(
                rotation.Data[6] * rotation.Data[6] + 
                rotation.Data[7] * rotation.Data[7]) * radius
        );
        
        Vector3 worldHalfSize = halfHeightContribution + radiusContribution;

        return new BoundingBox(
            center: primitive.Body.Position,
            halfSize: worldHalfSize);
    }
    
    public BoundingBox GetBoundingBox()
    {
        return GetGetBoundingBox(this, Radius, Height);
    }
}