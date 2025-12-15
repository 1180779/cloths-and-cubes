using Engine.RigidBodies;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Force
{
    public class ForceRegistry
    {
 
        public struct ForceRegistration
        {
            public RigidBody body;
            public IForceGenerator fg;
        };

        /**
        * Holds the list of registrations.
        */
        public List<ForceRegistration> Registry = new();


        /**
        * Registers the given force generator to apply to the
        * given body.
        */
        public void Add(RigidBody body, IForceGenerator fg)
        {
            ForceRegistration registration;
            registration.body = body;
            registration.fg = fg;
            Registry.Add(registration);
        }

        /**
        * Removes the given registered pair from the registry.
        * If the pair is not registered, this method will have
        * no effect.
        */
        public void Remove(RigidBody body, IForceGenerator fg)
        {
            Registry.RemoveAll(r => r.body == body && r.fg == fg);
        }

        /**
        * Clears all registrations from the registry. This will
        * not delete the bodies or the force generators
        * themselves, just the records of their connection.
        */
        public void Clear()
        {
            Registry.Clear();
        }

        /**
        * Calls all the force generators to update the forces of
        * their corresponding bodies.
        */
        public void updateForces(float duration)
        {
            foreach (ForceRegistration body in Registry)
            {
                body.fg.UpdateForce(body.body, duration);
            }
        }
    };
}
