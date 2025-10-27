using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Collision.Bounding_Volume_Hierarchy
{
    public static class MortonCodes
    {
        // Expands a 10-bit integer into 30 bits by inserting 2 zeros between each bit.
        public static uint ExpandBits(uint v)
        {
            v = (v * 0x00010001u) & 0xFF0000FFu;
            v = (v * 0x00000101u) & 0x0F00F00Fu;
            v = (v * 0x00000011u) & 0xC30C30C3u;
            v = (v * 0x00000005u) & 0x49249249u;
            return v;
        }

        public static ulong Encode(Vector3 position, Vector3 min, Vector3 max)
        {
            // Normalize position to [0, 1]
            Engine.Vector3 normalized = new();
            normalized.X = (position.X - min.X) / (max.X - min.X);
            normalized.Y = (position.Y - min.Y) / (max.Y - min.Y);
            normalized.Z = (position.Z - min.Z) / (max.Z - min.Z);


            uint x = (uint)Math.Min(Math.Max(normalized.X * 1024.0f, 0.0f), 1023.0f);
            uint y = (uint)Math.Min(Math.Max(normalized.Y * 1024.0f, 0.0f), 1023.0f);
            uint z = (uint)Math.Min(Math.Max(normalized.Z * 1024.0f, 0.0f), 1023.0f);
            uint xx = ExpandBits(x);
            uint yy = ExpandBits(y);
            uint zz = ExpandBits(z);
            return xx * 4 + yy * 2 + zz;
        }
    }
}
