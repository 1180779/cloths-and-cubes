using System.Diagnostics;
using System.Numerics;

namespace Engine;

public struct Vector3
{
    public static Vector3 Gravity = new Vector3(0, (Real)(-9.81), 0);
    public static Vector3 HighGravity = new Vector3(0, (Real)(-19.62), 0);
    public static Vector3 Up = new Vector3(0, 1, 0);
    public static Vector3 Right = new Vector3(1, 0, 0);
    public static Vector3 OutOfScreen = new Vector3(0, 0, 1);

    public static Vector3 UnitX = new Vector3(0, 1, 0);
    public static Vector3 UnitY = new Vector3(1, 0, 0);
    public static Vector3 UnitZ = new Vector3(0, 0, 1);

    public Real X, Y, Z;

    [Conditional("DEBUG")]
    public void DebugAssertNotNan()
    {
        Debug.Assert(!Real.IsNaN(X));
        Debug.Assert(!Real.IsNaN(Y));
        Debug.Assert(!Real.IsNaN(Z));

        Debug.Assert(!Real.IsInfinity(X));
        Debug.Assert(!Real.IsInfinity(Y));
        Debug.Assert(!Real.IsInfinity(Z));
    }

    public Vector3()
    {
    }

    public Vector3(Vector3 v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
    }

    public Vector3(Real x = 0, Real y = 0, Real z = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public void Invert()
    {
        X = -X;
        Y = -Y;
        Z = -Z;
    }

    public Real SqMagnitude => X * X + Y * Y + Z * Z;
    public Real Magnitude => Real.Sqrt(X * X + Y * Y + Z * Z);

    public void Normalize()
    {
        var mag = Magnitude;
        if (mag == 0) return;
        X /= mag;
        Y /= mag;
        Z /= mag;
    }

    public static Vector3 operator *(Vector3 v, Real scalar)
    {
        return new Vector3(v.X * scalar, v.Y * scalar, v.Z * scalar);
    }

    public static Real operator *(Vector3 v, Vector3 u)
    {
        return v.X * u.X + v.Y * u.Y + v.Z * u.Z;
    }


    public override string ToString()
    {
        return $"[{X:F2}, {Y:F2}, {Z:F2}]";
    }

    public static Real ScalarProduct(Vector3 v1, Vector3 v2)
    {
        return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
    }

    public Real ScalarProduct(Vector3 v) => ScalarProduct(this, v);


    public static Vector3 ComponentProduct(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
    }

    public static Vector3 CrossProduct(Vector3 v, Vector3 u)
    {
        return new Vector3(
            v.Y * u.Z - v.Z * u.Y,
            v.Z * u.X - v.X * u.Z,
            v.X * u.Y - v.Y * u.X
        );
    }

    public void ComponentProductUpdate(Vector3 v)
    {
        X *= v.X;
        Y *= v.Y;
        Z *= v.Z;
    }

    public static Vector3 operator +(Vector3 v, Vector3 u) => new Vector3(v.X + u.X, v.Y + u.Y, v.Z + u.Z);
    public static Vector3 operator -(Vector3 v, Vector3 u) => new Vector3(v.X - u.X, v.Y - u.Y, v.Z - u.Z);
    public static Vector3 operator %(Vector3 v, Vector3 u) => CrossProduct(v, u);

    public static (Vector3 v1, Vector3 v2, Vector3? v3) GetOrthogonalBasis(Vector3 u, Vector3 v)
    {
        Vector3 q = u % v;
        if (q.Magnitude == 0) return (u, v, null);
        v = q % u;
        u.Normalize();
        v.Normalize();
        q.Normalize();
        return (u, v, q);
    }

    public static Real[] ConcatAndNormalize(params Vector3[] vectors)
    {
        List<Real> result = new List<Real>();
        foreach (var v in vectors)
        {
            v.Normalize();
            result.Add(v.X);
            result.Add(v.Y);
            result.Add(v.Z);
        }

        return [.. result];
    }

    public static implicit operator Real[](Vector3 v)
    {
        return [v.X, v.Y, v.Z];
    }

    public static implicit operator Vector3(Real[] v)
    {
        Debug.Assert(v.Length == 3);
        return new Vector3(v[0], v[1], v[2]);
    }

    public static Vector3 RandomVector(System.Random random, Vector3 min, Vector3 max)
    {
        Real x = (Real)random.NextDouble() * (max.X - min.X) + min.X;
        Real y = (Real)random.NextDouble() * (max.Y - min.Y) + min.Y;
        Real z = (Real)random.NextDouble() * (max.Z - min.Z) + min.Z;

        return new Vector3(x, y, z);
    }

    public void Clear()
    {
        X = Y = Z = 0;
    }

    public Vector3 Normalise()
    {
        Real l = Magnitude;
        if (l > 0)
        {
            X /= l;
            Y /= l;
            Z /= l;
        }

        return this;
    }

    public void AddScaledVector(Vector3 vector, Real scale)
    {
        X += vector.X * scale;
        Y += vector.Y * scale;
        Z += vector.Z * scale;
    }

    public Real SquareMagnitude()
    {
        return X * X + Y * Y + Z * Z;
    }

    public static Vector3 VectorProduct(Vector3 o, Vector3 vector)
    {
        return new Vector3(o.Y * vector.Z - o.Z * vector.Y,
            o.Z * vector.X - o.X * vector.Z,
            o.X * vector.Y - o.Y * vector.X);
    }

    public Vector3 VectorProduct(Vector3 v) => VectorProduct(this, v);

    private Real GetValue<TKey>(TKey key) where TKey : IBinaryInteger<TKey>
    {
        int intKey = int.CreateChecked(key);
        if (intKey == 0)
        {
            return X;
        }

        if (intKey == 1)
        {
            return Y;
        }

        return Z;
    }

    private void SetValue<TKey>(TKey key, Real value) where TKey : IBinaryInteger<TKey>
    {
        int intKey = int.CreateChecked(key);
        if (intKey == 0)
        {
            X = value;
            return;
        }

        if (intKey == 1)
        {
            Y = value;
            return;
        }

        Z = value;
    }

    public Real this[int key]
    {
        get => GetValue(key);
        set => SetValue(key, value);
    }

    public Real this[uint key]
    {
        get => GetValue(key);
        set => SetValue(key, value);
    }
}