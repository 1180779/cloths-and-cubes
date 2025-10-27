using Engine.Collision;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.RigidBodies
{
    public class RigidParticle : CollisionParticle
    {
        public void SetState(
        Vector3 position,
        
        float extents,
        Vector3 velocity,
        float mass=0.1f)
        {
            Body.Position = position;


            Body.Velocity = velocity;
            Body.Rotation = new();

           

            
            Body.Mass = mass;

            Matrix3 tensor = new();

            Body.SetInertiaTensor(tensor);

            Body.LinearDamping = 0.95f;
            Body.AngularDamping = 0.8f;
            Body.ClearAccumulators();
            Body.Acceleration = new(0, -10f, 0);

            Body.SetAwake();

            Body.CalculateDerivedData();
        }
    }
}
