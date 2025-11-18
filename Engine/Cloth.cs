using Engine.Force;
using Engine.ParticleEngine;
using Engine.RigidBodies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class Cloth
    {
    public RigidParticle[,] particles;
    public ForceRegistry registry;
    public int sizeX;
    public int sizeY;
    public Vector3 particle0pos;
    public float springLength;
    public float springConstant;
    public float ParticleMass;
    public Cloth(ForceRegistry _registry, int sizeX = 10, int sizeY = 1, float springLength = 1f, float springConstant = 1f, float particleMass = 1f, Vector3 particle0pos = null)
    {
        if (particle0pos == null)
        {
            particle0pos = new Vector3(0f, 0f, 10f);
        }
        registry= _registry;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.springLength = springLength;
        this.springConstant = springConstant;
        this.ParticleMass = particleMass;
        this.particle0pos = particle0pos;
        particles = new RigidParticle[sizeX, sizeY];
        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                    particles[i,j]= new RigidParticle();
                    particles[i,j].SetState(particle0pos+new Vector3(springLength*i,springLength*j,0),0,new Vector3());
            }
        }
        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                if (i != sizeX - 1)
                {
                        registry.Add(particles[i, j].Body, new Spring(particles[i + 1, j].Body.Position, particles[i, j].Body, particles[i, j].Body.Position, springConstant, springLength));
                }
                if (j != sizeY - 1)
                {
                        registry.Add(particles[i, j].Body, new Spring(particles[i , j+1].Body.Position, particles[i, j].Body, particles[i, j].Body.Position, springConstant, springLength));
                }
                if (i != sizeX - 1 && j != sizeY - 1)
                {
                        registry.Add(particles[i, j].Body, new Spring(particles[i + 1, j+1].Body.Position, particles[i, j].Body, particles[i, j].Body.Position, springConstant, springLength));
                }
            }
        }
    }
    public void Pin(uint x, uint y, Vector3 pos)
    {
        if (x >= sizeX | y >= sizeY)
        {
            return;
        }
        //TODO

    }
    public void Move(Vector3 move)
    {
        //TODO
    }
    public void Rotate(Vector3 rot)
    {
        //TODO

    }

}
}
