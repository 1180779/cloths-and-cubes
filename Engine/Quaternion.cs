using System.Diagnostics;

namespace Engine;

public class Quaternion : ICloneable
{
    public Real I, J, K, R;

    [Conditional("DEBUG")]
    public void DebugAssertNotNan()
    {
        Debug.Assert(!Real.IsNaN(I));
        Debug.Assert(!Real.IsNaN(J));
        Debug.Assert(!Real.IsNaN(K));
        Debug.Assert(!Real.IsNaN(R));
        
        Debug.Assert(!Real.IsInfinity(I));
        Debug.Assert(!Real.IsInfinity(J));
        Debug.Assert(!Real.IsInfinity(K));
        Debug.Assert(!Real.IsInfinity(R));
    }
    
    public Quaternion(Real r, Real i, Real j, Real k)
    {
        this.R = r;
        this.I = i;
        this.J = j;
        this.K = k;
    }

    public Quaternion()
    {
        R = 1;
        I = 0;
        J = 0;
        K = 0;
    }

    public Real this[uint key]
    {
        get
        {
            return key switch
            {
                0 => R,
                1 => I,
                2 => J,
                3 => K,
                _ => throw new ArgumentOutOfRangeException(nameof(key), "Index must be between 0 and 3")
            };
        }
        set
        {
            switch (key)
            {
                case 0: R = value; break;
                case 1: I = value; break;
                case 2: J = value; break;
                case 3: K = value; break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(key), "Index must be between 0 and 3");
            }
        }
    }
    
    public void Normalise()
    {
        Real d = R * R + I * I + J * J + K * K;

        // Check for zero length quaternion, and use the no-rotation
        // quaternion in that case.
        if (d < Real.Epsilon)
        {
            R = 1;
            return;
        }

        d = (Real)(1.0) / (Real)Math.Sqrt(d);
        R *= d;
        I *= d;
        J *= d;
        K *= d;
    }

    public static Quaternion operator *(Quaternion q, Real s) => new Quaternion()
    {
        R = q.R * s,
        I = q.I * s,
        J = q.J * s,
        K = q.K * s
    };
    
    public static Quaternion operator +(Quaternion q, Vector3 v)
    {
        Quaternion t = new()
        {
            R = 0,
            I = v.X,
            J = v.Y,
            K = v.Z
        };
        t *= q;
        t *= 0.5f;
        return t;
    }
    
    public static Quaternion operator *(Quaternion q, Quaternion multiplier) =>
        new()
        {
            R = q.R * multiplier.R - q.I * multiplier.I -
                q.J * multiplier.J - q.K * multiplier.K,
            I = q.R * multiplier.I + q.I * multiplier.R +
                q.J * multiplier.K - q.K * multiplier.J,
            J = q.R * multiplier.J + q.J * multiplier.R +
                q.K * multiplier.I - q.I * multiplier.K,
            K = q.R * multiplier.K + q.K * multiplier.R +
                q.I * multiplier.J - q.J * multiplier.I
        };

    public void AddScaledVector(Vector3 vector, Real scale)
    {
        Quaternion q = new Quaternion(0,
            vector.X * scale,
            vector.Y * scale,
            vector.Z * scale);
        q *= this;
        R += q.R * 0.5f;
        I += q.I * 0.5f;
        J += q.J * 0.5f;
        K += q.K * 0.5f;
    }

    public void RotateByVector(Vector3 vector)
    {
        Quaternion q = new Quaternion(0, vector.X, vector.Y,
            vector.Z);
        Quaternion p = this;
        p *= q;
        R = p.R;
        J = p.J;
        K = p.K;
        I = p.I;
    }

    public object Clone()
    {
        return new Quaternion(R, I, J, K);
    }
}