using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Collision
{
    public class CollisionParticle : CollisionPrimitive
    {
        public Vector3 Radius { get; set; } = new();
    }
}
