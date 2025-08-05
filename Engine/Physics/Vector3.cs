using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Physics
{
    public class Vector3
    {
        public static Vector3 GRAVITY = new  Vector3(0, (Real)(-9.81), 0);
        public static Vector3 HIGH_GRAVITY = new  Vector3(0, (Real)(-19.62), 0);
        public static Vector3 UP = new  Vector3(0, 1, 0);
        public static Vector3 RIGHT = new  Vector3(1, 0, 0);
        public static Vector3 OUT_OF_SCREEN = new  Vector3(0, 0, 1);
        public static Vector3 X = new  Vector3(0, 1, 0);
        public static Vector3 Y = new  Vector3(1, 0, 0);
        public static Vector3 Z = new  Vector3(0, 0, 1);



        public Real x, y, z;
        private Real pad;

        public Vector3(Real _x = 0, Real _y = 0, Real _z = 0)
        {
            x = _x;
            y = _y;
            z = _z;
        }


        public void Invert()
        {
            x = -x;
            y = -y;
            z = -z;
        }

        public Real SqMagnitude()
        {
            return x * x + y * y + z * z;
        }

        public Real Magnitude()
        {
            return Real.Sqrt(x*x + y*y + z*z);
        }

        public void Normalize()
        {
            var mag = Magnitude();
            if(mag == 0)  return;
            x /= mag;
            y /= mag;
            z /= mag;
        }

        public static Vector3 operator*(Vector3 v, Real scalar)
        {
            return new Vector3(v.x*scalar, v.y*scalar, v.z*scalar);
        }

        public static Real operator*(Vector3 v, Vector3 u)
        {
            return v.x*u.x + v.y * u.y + v.z*u.z;
        }


        public override string ToString()
        {
            return $"[{x:F2}, {y:F2}, {z:F2}]";
        }

        public static Vector3 ComponentProduct(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x*v2.x, v1.y * v2.y, v1.z*v2.z);
        }

        public static Vector3 CrossProduct(Vector3 v, Vector3 u)
        {
            return new Vector3(
                v.y * u.z - v.z * u.y,
                v.z * u.x - v.x * u.z,
                v.x * u.y - v.y * u.x
                );
        }

        public void ComponentProductUpdate(Vector3 v)
        {
            var temp = ComponentProduct(this, v);
            x = temp.x;
            y = temp.y;
            z = temp.z;
        }

        public static Vector3 operator+(Vector3 v, Vector3 u) => new Vector3(v.x + u.x, v.y + u.y, v.z + u.z);
        public static Vector3 operator-(Vector3 v, Vector3 u) => new Vector3(v.x - u.x, v.y - u.y, v.z - u.z);
        public static Vector3 operator%(Vector3 v, Vector3 u) => CrossProduct(v, u);

        public static (Vector3 v1, Vector3 v2, Vector3? v3) GetOrthogonalBasis(Vector3 u, Vector3 v)
        {
            Vector3 q = u % v;
            if (q.Magnitude() == 0) return (u, v, null);
            v = q % u;
            u.Normalize();
            v.Normalize();
            q.Normalize();
            return (u, v, q);
        }

        public static float[] ConcatAndNormalize(params Vector3[] vectors)
        {
            List<float> result = new List<float>();
            foreach (var v in vectors)
            {
                v.Normalize();
                result.Add(v.x);
                result.Add(v.y);
                result.Add(v.z);               
            }
            return [.. result];
        }

        public static implicit operator Real[](Vector3 v)
        {
            return [v.x, v.y, v.z];
        }

        public static implicit operator Vector3(Real[] v)
        {
            Debug.Assert(v.Length == 3);
            return new Vector3(v[0], v[1], v[2]);
        }

        public static Vector3 RandomVector(Random random, Vector3 min, Vector3 max)
        {
            Real x = (Real)random.NextDouble() * (max.x - min.x) + min.x;
            Real y = (Real)random.NextDouble() * (max.y - min.y) + min.y;
            Real z = (Real)random.NextDouble() * (max.z - min.z) + min.z;
            
            return new Vector3(x, y, z);
        }
    }
}
