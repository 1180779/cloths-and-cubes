using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Collision.ContactGraph
{
    public class ContactGraph
    {
        // Margin of error when determining if two contacts are close enough to affect each other; needs to be fine-tuned
        //private const float _tolerance = 1.0f;
        private List<ContactGraphComponent> _components = [];
        public List<ContactGraphComponent> Components => _components;
        public ContactGraph()
        {
        }

        public void AddContact(Contact contact)
        {
            // If either body is null, handle as a static contact
            if (contact.Body[0] == null || contact.Body[1] == null)
            {
                if (contact.Body[0] == null && contact.Body[1] == null)
                {
                    return;
                }
                foreach (var item in _components)
                {
                    // Check if either body in the contact is already represented in this component
                    if ((contact.Body[0] != null && item.Bodies.Contains(contact.Body[0]!)) || (contact.Body[1] != null && item.Bodies.Contains(contact.Body[1]!)))
                    {
                        item.AddStaticContact(contact);
                        return;
                    }
                }

                ContactGraphComponent newComponentStatic = new ContactGraphComponent();
                newComponentStatic.AddStaticContact(contact);
                _components.Add(newComponentStatic);
                return;
            }
            else
            {
                int compAIndex = -1;
                int compBIndex = -1;
                // Check existing components to see if either body is already represented
                for(int i = 0; i < _components.Count; i++)
                {
                    var item = _components[i];
                    if (item.Bodies.Contains(contact.Body[0]!))
                    {
                        compAIndex = i;
                    }
                    if (item.Bodies.Contains(contact.Body[1]!))
                    {
                        compBIndex = i;
                    }
                    if(compAIndex != -1 && compBIndex != -1)
                    {
                        break;
                    }   
                }
                if(compAIndex != -1 && compBIndex != -1)
                {
                    if (compAIndex != compBIndex)
                    {
                        // Merge the two components
                        var compA = _components[compAIndex];
                        var compB = _components[compBIndex];
                        compA.AddContact(contact);
                        compA += compB;
                        _components.RemoveAt(compBIndex);
                    }
                    else
                    {
                        // Both bodies are already in the same component
                        _components[compAIndex].AddContact(contact);
                    }
                    return;
                }
                else if (compAIndex != -1)
                {
                    // Only body A is represented; add contact to that component
                    _components[compAIndex].AddContact(contact);
                    return;
                }
                else if (compBIndex != -1)
                {
                    // Only body B is represented; add contact to that component
                    _components[compBIndex].AddContact(contact);
                    return;
                }
            }


            // If neither body is represented in any existing component, create a new component
            ContactGraphComponent newComponent = new ContactGraphComponent();
            newComponent.AddContact(contact);
            _components.Add(newComponent);
        }

        public static ContactGraph Build(Contact[] contacts, uint numContacts)
        {
            ContactGraph graph = new ContactGraph();
            for(int i = 0; i<numContacts; i++)
            {
                graph.AddContact(contacts[i]);
            }
            return graph;
        }

        public void ResolvePositions(uint maxPositionIterations, float positionEpsilon)
        {
            Parallel.ForEach(Components, comp => comp.ResolvePositions(maxPositionIterations, positionEpsilon));
        }

        public void ResolveVelocities(uint maxVelocityIterations, float velocityEpsilon, Real duration)
        {
            Parallel.ForEach(Components, comp => comp.ResolveVelocities(maxVelocityIterations, velocityEpsilon, duration));
        }

        //public static (Vector3 min, Vector3 max) GetContactBounds(Contact contact)
        //{

        //}
    }
}
