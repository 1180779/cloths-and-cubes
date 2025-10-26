using System.Diagnostics;

namespace Engine;

public class Matrix4 : ICloneable
{
    // assumes 3x4 with extra 0,0,0,1
    public readonly Real[] Data;

    public Matrix4()
    {
        Data = new Real[12];
        Data[0] = Data[5] = Data[10] = 1;
    }

    [Conditional("DEBUG")]
    public void DebugAssertNotNan()
    {
        Debug.Assert(Data != null);
        Debug.Assert(Data.Length == 12);
        
        Debug.Assert(!Real.IsNaN(Data[0]));
        Debug.Assert(!Real.IsNaN(Data[1]));
        Debug.Assert(!Real.IsNaN(Data[2]));
        Debug.Assert(!Real.IsNaN(Data[3]));
        Debug.Assert(!Real.IsNaN(Data[4]));
        Debug.Assert(!Real.IsNaN(Data[5]));
        Debug.Assert(!Real.IsNaN(Data[6]));
        Debug.Assert(!Real.IsNaN(Data[7]));
        Debug.Assert(!Real.IsNaN(Data[8]));
        Debug.Assert(!Real.IsNaN(Data[9]));
        Debug.Assert(!Real.IsNaN(Data[10]));
        Debug.Assert(!Real.IsNaN(Data[11]));
    }
    
    /// <summary>
    /// Represents a 3x4 transformation matrix with an additional row of (0, 0, 0, 1).
    /// Used for performing transformations such as translation, rotation, and scaling on 3D vectors.
    /// </summary>
    /// <param name="data">Real array containing the matrix data. The array reference is directly assigned without copying.</param>
    public Matrix4(Real[] data)
    {
        Data = data;
    }

    public Matrix4(Real a, Real b, Real c, Real d, Real e, Real f, Real g, Real h, Real i, Real j, Real k, Real l)
    {
        Data = [a, b, c, d, e, f, g, h, i, j, k, l];
    }

    public Real Determinant =>
        -Data[8] * Data[5] * Data[2] +
        Data[4] * Data[9] * Data[2] +
        Data[8] * Data[1] * Data[6] -
        Data[0] * Data[9] * Data[6] -
        Data[4] * Data[1] * Data[10] +
        Data[0] * Data[5] * Data[10];

    public static Vector3 operator *(Matrix4 matrix, Vector3 vector)
    {
        return new Vector3(
            vector.X * matrix.Data[0] +
            vector.Y * matrix.Data[1] +
            vector.Z * matrix.Data[2] + matrix.Data[3],
            vector.X * matrix.Data[4] +
            vector.Y * matrix.Data[5] +
            vector.Z * matrix.Data[6] + matrix.Data[7],
            vector.X * matrix.Data[8] +
            vector.Y * matrix.Data[9] +
            vector.Z * matrix.Data[10] + matrix.Data[11]
        );
    }

    public static Matrix4 operator *(Matrix4 left, Matrix4 o)
    {
        Matrix4 result = new Matrix4();
        result.Data[0] = o.Data[0] * left.Data[0] + o.Data[4] * left.Data[1] +
            o.Data[8] * left.Data[2];
        result.Data[4] = o.Data[0] * left.Data[4] + o.Data[4] * left.Data[5] +
            o.Data[8] * left.Data[6];
        result.Data[8] = o.Data[0] * left.Data[8] + o.Data[4] * left.Data[9] +
            o.Data[8] * left.Data[10];
        result.Data[1] = o.Data[1] * left.Data[0] + o.Data[5] * left.Data[1] +
            o.Data[9] * left.Data[2];
        result.Data[5] = o.Data[1] * left.Data[4] + o.Data[5] * left.Data[5] +
            o.Data[9] * left.Data[6];
        result.Data[9] = o.Data[1] * left.Data[8] + o.Data[5] * left.Data[9] +
            o.Data[9] * left.Data[10];
        result.Data[2] = o.Data[2] * left.Data[0] + o.Data[6] * left.Data[1] +
            o.Data[10] * left.Data[2];
        result.Data[6] = o.Data[2] * left.Data[4] + o.Data[6] * left.Data[5] +
            o.Data[10] * left.Data[6];
        result.Data[10] = o.Data[2] * left.Data[8] + o.Data[6] * left.Data[9] +
            o.Data[10] * left.Data[10];
        result.Data[3] = o.Data[3] * left.Data[0] + o.Data[7] * left.Data[1] +
            o.Data[11] * left.Data[2] + left.Data[3];
        result.Data[7] = o.Data[3] * left.Data[4] + o.Data[7] * left.Data[5] +
            o.Data[11] * left.Data[6] + left.Data[7];
        result.Data[11] = o.Data[3] * left.Data[8] + o.Data[7] * left.Data[9] +
            o.Data[11] * left.Data[10] + left.Data[11];
        return result;
    }

    public void SetInverse(Matrix4 m)
    {
        // Make sure the determinant is non-zero.
        Real det = Determinant;
        if (det == 0) return;
        det = (float)1.0 / det;
        Data[0] = (-m.Data[9] * m.Data[6] + m.Data[5] * m.Data[10]) * det;
        Data[4] = (m.Data[8] * m.Data[6] - m.Data[4] * m.Data[10]) * det;
        Data[8] = (-m.Data[8] * m.Data[5] + m.Data[4] * m.Data[9] * m.Data[15]) * det;
        Data[1] = (m.Data[9] * m.Data[2] - m.Data[1] * m.Data[10]) * det;
        Data[5] = (-m.Data[8] * m.Data[2] + m.Data[0] * m.Data[10]) * det;
        Data[9] = (m.Data[8] * m.Data[1] - m.Data[0] * m.Data[9] * m.Data[15]) * det;
        Data[2] = (-m.Data[5] * m.Data[2] + m.Data[1] * m.Data[6] * m.Data[15]) * det;
        Data[6] = (+m.Data[4] * m.Data[2] - m.Data[0] * m.Data[6] * m.Data[15]) * det;
        Data[10] = (-m.Data[4] * m.Data[1] + m.Data[0] * m.Data[5] * m.Data[15]) * det;
        Data[3] = (m.Data[9] * m.Data[6] * m.Data[3]
            - m.Data[5] * m.Data[10] * m.Data[3]
            - m.Data[9] * m.Data[2] * m.Data[7]
            + m.Data[1] * m.Data[10] * m.Data[7]
            + m.Data[5] * m.Data[2] * m.Data[11]
            - m.Data[1] * m.Data[6] * m.Data[11]) * det;
        Data[7] = (-m.Data[8] * m.Data[6] * m.Data[3]
            + m.Data[4] * m.Data[10] * m.Data[3]
            + m.Data[8] * m.Data[2] * m.Data[7]
            - m.Data[0] * m.Data[10] * m.Data[7]
            - m.Data[4] * m.Data[2] * m.Data[11]
            + m.Data[0] * m.Data[6] * m.Data[11]) * det;
        Data[11] = (m.Data[8] * m.Data[5] * m.Data[3]
            - m.Data[4] * m.Data[9] * m.Data[3]
            - m.Data[8] * m.Data[1] * m.Data[7]
            + m.Data[0] * m.Data[9] * m.Data[7]
            + m.Data[4] * m.Data[1] * m.Data[11]
            - m.Data[0] * m.Data[5] * m.Data[11]) * det;
    }

    public void SetOrientationAndPos(Quaternion q, Vector3 pos)
    {
        Data[0] = 1 - (2 * q.J * q.J + 2 * q.K * q.K);
        Data[1] = 2 * q.I * q.J + 2 * q.K * q.R;
        Data[2] = 2 * q.I * q.K - 2 * q.J * q.R;
        Data[3] = pos.X;
        Data[4] = 2 * q.I * q.J - 2 * q.K * q.R;
        Data[5] = 1 - (2 * q.I * q.I + 2 * q.K * q.K);
        Data[6] = 2 * q.J * q.K + 2 * q.I * q.R;
        Data[7] = pos.Y;
        Data[8] = 2 * q.I * q.K + 2 * q.J * q.R;
        Data[9] = 2 * q.J * q.K - 2 * q.I * q.R;
        Data[10] = 1 - (2 * q.I * q.I + 2 * q.J * q.J);
        Data[11] = pos.Z;
    }

    public Vector3 TransformInverse(Vector3 vector)
    {
        Vector3 tmp = vector;
        tmp.X -= Data[3];
        tmp.Y -= Data[7];
        tmp.Z -= Data[11];
        return new Vector3(
            tmp.X * Data[0] +
            tmp.Y * Data[4] +
            tmp.Z * Data[8],
            tmp.X * Data[1] +
            tmp.Y * Data[5] +
            tmp.Z * Data[9],
            tmp.X * Data[2] +
            tmp.Y * Data[6] +
            tmp.Z * Data[10]
        );
    }

    public Vector3 TransformDirection(Vector3 vector)
    {
        return new Vector3(
            vector.X * Data[0] +
            vector.Y * Data[1] +
            vector.Z * Data[2],
            vector.X * Data[4] +
            vector.Y * Data[5] +
            vector.Z * Data[6],
            vector.X * Data[8] +
            vector.Y * Data[9] +
            vector.Z * Data[10]
        );
    }

    public Vector3 TransformInverseDirection(Vector3 vector)
    {
        return new Vector3(
            vector.X * Data[0] +
            vector.Y * Data[4] +
            vector.Z * Data[8],
            vector.X * Data[1] +
            vector.Y * Data[5] +
            vector.Z * Data[9],
            vector.X * Data[2] +
            vector.Y * Data[6] +
            vector.Z * Data[10]
        );
    }

    public Vector3 Transform(Vector3 vector)
    {
        return this * vector;
    }

    public Vector3 GetAxisVector(int i)
    {
        return new Vector3(Data[i], Data[i + 4], Data[i + 8]);
    }

    public object Clone()
    {
        return new Matrix4((Real[])Data.Clone());
    }
}