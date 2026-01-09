using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Engine;

public struct Matrix3 : ICloneable
{
    public Real[] Data;
    public const int ExpectedSize = 9;

    public Matrix3()
    {
        Data = new Real[ExpectedSize];
    }

    public Matrix3(Real[] data)
    {
        Debug.Assert(data.Length == ExpectedSize);
        Data = data;
    }

    public Matrix3(
        Real i0,
        Real i1,
        Real i2,
        Real i3,
        Real i4,
        Real i5,
        Real i6,
        Real i7,
        Real i8)
    {
        Data = [i0, i1, i2, i3, i4, i5, i6, i7, i8];
    }

    /// <summary>
    /// Retrieves the specified axis of the matrix as a <see cref="Vector3"/>.
    /// </summary>
    /// <param name="index">The index of the axis to retrieve
    /// (0 for the first column, 1 for the second column, 2 for the third column).</param>
    /// <returns>A <see cref="Vector3"/> representing the axis of the matrix corresponding to the specified index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Axis(int index)
    {
        return new Vector3(Data[index], Data[index + 3], Data[index + 6]);
    }

    public Real this[int i]
    {
        get
        {
            Debug.Assert(i >= 0 && i < ExpectedSize);
            return Data[i];
        }
        set
        {
            Debug.Assert(i >= 0 && i < ExpectedSize);
            Data[i] = value;
        }
    }

    public static Matrix3 operator +(Matrix3 left, Matrix3 right)
    {
        Matrix3 result = new();
        for (var i = 0; i < ExpectedSize; i++)
        {
            result.Data[i] = left.Data[i] + right.Data[i];
        }

        return result;
    }

    public static Matrix3 operator *(Matrix3 m, float s)
    {
        Matrix3 result = new();
        for (var i = 0; i < ExpectedSize; i++)
        {
            result.Data[i] = m[i] * s;
        }

        return result;
    }

    public static Matrix3 operator *(Matrix3 left, Matrix3 o)
    {
        return new Matrix3([
                left.Data[0] * o.Data[0] + left.Data[1] * o.Data[3] + left.Data[2] * o.Data[6],
                left.Data[0] * o.Data[1] + left.Data[1] * o.Data[4] + left.Data[2] * o.Data[7],
                left.Data[0] * o.Data[2] + left.Data[1] * o.Data[5] + left.Data[2] * o.Data[8],

                left.Data[3] * o.Data[0] + left.Data[4] * o.Data[3] + left.Data[5] * o.Data[6],
                left.Data[3] * o.Data[1] + left.Data[4] * o.Data[4] + left.Data[5] * o.Data[7],
                left.Data[3] * o.Data[2] + left.Data[4] * o.Data[5] + left.Data[5] * o.Data[8],

                left.Data[6] * o.Data[0] + left.Data[7] * o.Data[3] + left.Data[8] * o.Data[6],
                left.Data[6] * o.Data[1] + left.Data[7] * o.Data[4] + left.Data[8] * o.Data[7],
                left.Data[6] * o.Data[2] + left.Data[7] * o.Data[5] + left.Data[8] * o.Data[8]
            ]
        );
    }

    public void SetComponents(
        Vector3 compOne,
        Vector3 compTwo,
        Vector3 compThree)
    {
        Data[0] = compOne.X;
        Data[1] = compTwo.X;
        Data[2] = compThree.X;
        Data[3] = compOne.Y;
        Data[4] = compTwo.Y;
        Data[5] = compThree.Y;
        Data[6] = compOne.Z;
        Data[7] = compTwo.Z;
        Data[8] = compThree.Z;
    }

    public void SetInverse(Matrix3 m)
    {
        Real t4 = m.Data[0] * m.Data[4];
        Real t6 = m.Data[0] * m.Data[5];
        Real t8 = m.Data[1] * m.Data[3];
        Real t10 = m.Data[2] * m.Data[3];
        Real t12 = m.Data[1] * m.Data[6];
        Real t14 = m.Data[2] * m.Data[6];

        // Calculate the determinant
        Real t16 = t4 * m.Data[8] - t6 * m.Data[7] - t8 * m.Data[8] +
            t10 * m.Data[7] + t12 * m.Data[5] - t14 * m.Data[4];

        // Make sure the determinant is non-zero.
        if (t16 == (Real)0.0f) return;
        Real t17 = 1 / t16;

        Data[0] = (m.Data[4] * m.Data[8] - m.Data[5] * m.Data[7]) * t17;
        Data[1] = -(m.Data[1] * m.Data[8] - m.Data[2] * m.Data[7]) * t17;
        Data[2] = (m.Data[1] * m.Data[5] - m.Data[2] * m.Data[4]) * t17;
        Data[3] = -(m.Data[3] * m.Data[8] - m.Data[5] * m.Data[6]) * t17;
        Data[4] = (m.Data[0] * m.Data[8] - t14) * t17;
        Data[5] = -(t6 - t10) * t17;
        Data[6] = (m.Data[3] * m.Data[7] - m.Data[4] * m.Data[6]) * t17;
        Data[7] = -(m.Data[0] * m.Data[7] - t12) * t17;
        Data[8] = (t4 - t8) * t17;
    }

    public Matrix3 Inverse
    {
        get
        {
            Matrix3 result = new Matrix3();
            result.SetInverse(this);
            return result;
        }
    }

    private void Invert()
    {
        SetInverse(this);
    }

    /// <summary>
    /// Sets the matrix to be a skew symmetric matrix based on
    /// the given vector. The skew symmetric matrix is the equivalent 
    /// of the vector product. So if a,b are vectors. a x b = A_s b
    /// where A_s is the skew symmetric form of a.
    /// </summary>
    public void SetSkewSymmetric(Vector3 v)
    {
        Data[0] = Data[4] = Data[8] = 0;
        Data[1] = -v.Z;
        Data[2] = v.Y;
        Data[3] = v.Z;
        Data[5] = -v.X;
        Data[6] = -v.Y;
        Data[7] = v.X;
    }

    private void SetTranspose(Matrix3 m)
    {
        Data[0] = m.Data[0];
        Data[1] = m.Data[3];
        Data[2] = m.Data[6];
        Data[3] = m.Data[1];
        Data[4] = m.Data[4];
        Data[5] = m.Data[7];
        Data[6] = m.Data[2];
        Data[7] = m.Data[5];
        Data[8] = m.Data[8];
    }

    public Matrix3 Transpose
    {
        get
        {
            Matrix3 result = new Matrix3();
            result.SetTranspose(this);
            return result;
        }
    }

    public void SetOrientation(Quaternion q)
    {
        Data[0] = 1 - (2 * q.J * q.J + 2 * q.K * q.K);
        Data[1] = 2 * q.I * q.J + 2 * q.K * q.R;
        Data[2] = 2 * q.I * q.K - 2 * q.J * q.R;
        Data[3] = 2 * q.I * q.J - 2 * q.K * q.R;
        Data[4] = 1 - (2 * q.I * q.I + 2 * q.K * q.K);
        Data[5] = 2 * q.J * q.K + 2 * q.I * q.R;
        Data[6] = 2 * q.I * q.K + 2 * q.J * q.R;
        Data[7] = 2 * q.J * q.K - 2 * q.I * q.R;
        Data[8] = 1 - (2 * q.I * q.I + 2 * q.J * q.J);
    }

    public static Vector3 operator *(Matrix3 matrix3, Vector3 vector)
    {
        return new Vector3(
            vector.X * matrix3.Data[0] + vector.Y * matrix3.Data[1] + vector.Z * matrix3.Data[2],
            vector.X * matrix3.Data[3] + vector.Y * matrix3.Data[4] + vector.Z * matrix3.Data[5],
            vector.X * matrix3.Data[6] + vector.Y * matrix3.Data[7] + vector.Z * matrix3.Data[8]
        );
    }

    public Vector3 Transform(Vector3 vector)
    {
        return this * vector;
    }

    public Vector3 TransformTranspose(Vector3 vector)
    {
        return new Vector3(
            vector.X * Data[0] + vector.Y * Data[3] + vector.Z * Data[6],
            vector.X * Data[1] + vector.Y * Data[4] + vector.Z * Data[7],
            vector.X * Data[2] + vector.Y * Data[5] + vector.Z * Data[8]
        );
    }

    /// <summary>
    ///Sets the value of the matrix from inertia tensor values.
    /// </summary>
    public void SetInertiaTensorCoefficients(
        Real ix,
        Real iy,
        Real iz,
        Real ixy = 0,
        Real ixz = 0,
        Real iyz = 0)
    {
        Data[0] = ix;
        Data[1] = Data[3] = -ixy;
        Data[2] = Data[6] = -ixz;
        Data[4] = iy;
        Data[5] = Data[7] = -iyz;
        Data[8] = iz;
    }

    /// <summary>
    /// Sets the value of the matrix as an inertia tensor of
    /// a rectangular block aligned with the body's coordinate
    /// system with the given axis half-sizes and mass.
    /// </summary>
    public void SetBlockInertiaTensor(Vector3 halfSizes, float mass)
    {
        Vector3 squares = Vector3.ComponentProduct(halfSizes, halfSizes);
        SetInertiaTensorCoefficients(0.3f * mass * (squares.Y + squares.Z),
            0.3f * mass * (squares.X + squares.Z),
            0.3f * mass * (squares.X + squares.Y));
    }

    public void SetSphereInertiaTensor(Real radius, Real mass)
    {
        Real coeff = 0.4f * mass * radius * radius;
        SetInertiaTensorCoefficients(coeff, coeff, coeff);
    }

    public object Clone()
    {
        return new Matrix3((Real[])Data.Clone());
    }
}