namespace Engine.Physics
{
    public class Matrix3
    {

        public float[] data;
        Matrix3()
        {
            data = new float[12];
        }
        Matrix3(float[] data)
        {
            data = data;
        }
        Matrix3(float a, float b, float c, float d, float e, float f, float g, float h, float i)
        {
            data = [a, b, c, d, e, f, g, h, i];
        }
        public static Matrix3 operator *(Matrix3 left, Matrix3 o)
        {
            return new Matrix3([
            left.data[0] * o.data[0] +  left.data[1] * o.data[3] +  left.data[2] * o.data[6],
             left.data[0] * o.data[1] +  left.data[1] * o.data[4] +  left.data[2] * o.data[7],
             left.data[0] * o.data[2] +  left.data[1] * o.data[5] +  left.data[2] * o.data[8],
             left.data[3] * o.data[0] +  left.data[4] * o.data[3] +  left.data[5] * o.data[6],
             left.data[3] * o.data[1] +  left.data[4] * o.data[4] +  left.data[5] * o.data[7],
             left.data[3] * o.data[2] +  left.data[4] * o.data[5] +  left.data[5] * o.data[8],
             left.data[6] * o.data[0] +  left.data[7] * o.data[3] +  left.data[8] * o.data[6],
             left.data[6] * o.data[1] +  left.data[7] * o.data[4] +  left.data[8] * o.data[7],
             left.data[6] * o.data[2] +  left.data[7] * o.data[5] +  left.data[8] * o.data[8]
            ]
            );
        }
        public void setInverse(Matrix3 m)
        {
            float t4 = m.data[0] * m.data[4];
            float t6 = m.data[0] * m.data[5];
            float t8 = m.data[1] * m.data[3];
            float t10 = m.data[2] * m.data[3];
            float t12 = m.data[1] * m.data[6];
            float t14 = m.data[2] * m.data[6];
            // Calculate the determinant.
            float t16 = (t4 * m.data[8] - t6 * m.data[7] - t8 * m.data[8] + t10 * m.data[7] + t12 * m.data[5] - t14 * m.data[4]);
            // Make sure the determinant is non-zero.
            if (t16 == (float)0.0f) return;
            float t17 = 1 / t16;
            data[0] = (m.data[4] * m.data[8] - m.data[5] * m.data[7]) * t17;
            data[1] = -(m.data[1] * m.data[8] - m.data[2] * m.data[7]) * t17;
            data[2] = (m.data[1] * m.data[5] - m.data[2] * m.data[4]) * t17;
            data[3] = -(m.data[3] * m.data[8] - m.data[5] * m.data[6]) * t17;
            data[4] = (m.data[0] * m.data[8] - t14) * t17;
            data[5] = -(t6 - t10) * t17;
            data[6] = (m.data[3] * m.data[7] - m.data[4] * m.data[6]) * t17;
            data[7] = -(m.data[0] * m.data[7] - t12) * t17;
            data[8] = (t4 - t8) * t17;
        }
        Matrix3 inverse()
        {
            Matrix3 result = new Matrix3();
            result.setInverse(this);
            return result;
        }
        void invert()
        {
            setInverse(this);
        }
        void setTranspose(Matrix3 m)
        {
            data[0] = m.data[0];
            data[1] = m.data[3];
            data[2] = m.data[6];
            data[3] = m.data[1];
            data[4] = m.data[4];
            data[5] = m.data[7];
            data[6] = m.data[2];
            data[7] = m.data[5];
            data[8] = m.data[8];
        }
        Matrix3 transpose()
        {
            Matrix3 result = new Matrix3();
            result.setTranspose(this);
            return result;
        }
        void setOrientation(Quaternion q)
        {
            data[0] = 1 - (2 * q.j * q.j + 2 * q.k * q.k);
            data[1] = 2 * q.i * q.j + 2 * q.k * q.r;
            data[2] = 2 * q.i * q.k - 2 * q.j * q.r;
            data[3] = 2 * q.i * q.j - 2 * q.k * q.r;
            data[4] = 1 - (2 * q.i * q.i + 2 * q.k * q.k);
            data[5] = 2 * q.j * q.k + 2 * q.i * q.r;
            data[6] = 2 * q.i * q.k + 2 * q.j * q.r;
            data[7] = 2 * q.j * q.k - 2 * q.i * q.r;
            data[8] = 1 - (2 * q.i * q.i + 2 * q.j * q.j);
        }


    }
    public class Matrix4
    {
        //assumes 3x4 with extra 0,0,0,1
        public float[] data;
        Matrix4()
        {
            data = new float[12];
        }
        Matrix4(float[] data)
        {
            data = data;
        }
        Matrix4(float a, float b, float c, float d, float e, float f, float g, float h, float i, float j, float k, float l)
        {
            data = [a, b, c, d, e, f, g, h, i, j, k, l];
        }
        float getDeterminant()
        {
            return -data[8] * data[5] * data[2] +
        data[4] * data[9] * data[2] +
        data[8] * data[1] * data[6] -
        data[0] * data[9] * data[6] -
        data[4] * data[1] * data[10] +
        data[0] * data[5] * data[10];
        }
        public static Vector3 operator *(Matrix4 matrix, Vector3 vector)
        {
            return new Vector3(
            vector.x * matrix.data[0] +
            vector.y * matrix.data[1] +
            vector.z * matrix.data[2] + matrix.data[3],
            vector.x * matrix.data[4] +
            vector.y * matrix.data[5] +
            vector.z * matrix.data[6] + matrix.data[7],
            vector.x * matrix.data[8] +
            vector.y * matrix.data[9] +
            vector.z * matrix.data[10] + matrix.data[11]
            );

        }
        public static Matrix4 operator *(Matrix4 left, Matrix4 o)
        {
            Matrix4 result = new Matrix4();
            result.data[0] = (o.data[0] * left.data[0]) + (o.data[4] * left.data[1]) +
            (o.data[8] * left.data[2]);
            result.data[4] = (o.data[0] * left.data[4]) + (o.data[4] * left.data[5]) +
            (o.data[8] * left.data[6]);
            result.data[8] = (o.data[0] * left.data[8]) + (o.data[4] * left.data[9]) +
            (o.data[8] * left.data[10]);
            result.data[1] = (o.data[1] * left.data[0]) + (o.data[5] * left.data[1]) +
            (o.data[9] * left.data[2]);
            result.data[5] = (o.data[1] * left.data[4]) + (o.data[5] * left.data[5]) +
            (o.data[9] * left.data[6]);
            result.data[9] = (o.data[1] * left.data[8]) + (o.data[5] * left.data[9]) +
            (o.data[9] * left.data[10]);
            result.data[2] = (o.data[2] * left.data[0]) + (o.data[6] * left.data[1]) +
            (o.data[10] * left.data[2]);
            result.data[6] = (o.data[2] * left.data[4]) + (o.data[6] * left.data[5]) +
            (o.data[10] * left.data[6]);
            result.data[10] = (o.data[2] * left.data[8]) + (o.data[6] * left.data[9]) +
            (o.data[10] * left.data[10]);
            result.data[3] = (o.data[3] * left.data[0]) + (o.data[7] * left.data[1]) +
            (o.data[11] * left.data[2]) + left.data[3];
            result.data[7] = (o.data[3] * left.data[4]) + (o.data[7] * left.data[5]) +
            (o.data[11] * left.data[6]) + left.data[7];
            result.data[11] = (o.data[3] * left.data[8]) + (o.data[7] * left.data[9]) +
            (o.data[11] * left.data[10]) + left.data[11];
            return result;
        }
        void setInverse(Matrix4 m)
        {
            // Make sure the determinant is non-zero.
            float det = getDeterminant();
            if (det == 0) return;
            det = ((float)1.0) / det;
            data[0] = (-m.data[9] * m.data[6] + m.data[5] * m.data[10]) * det;
            data[4] = (m.data[8] * m.data[6] - m.data[4] * m.data[10]) * det;
            data[8] = (-m.data[8] * m.data[5] + m.data[4] * m.data[9] * m.data[15]) * det;
            data[1] = (m.data[9] * m.data[2] - m.data[1] * m.data[10]) * det;
            data[5] = (-m.data[8] * m.data[2] + m.data[0] * m.data[10]) * det;
            data[9] = (m.data[8] * m.data[1] - m.data[0] * m.data[9] * m.data[15]) * det;
            data[2] = (-m.data[5] * m.data[2] + m.data[1] * m.data[6] * m.data[15]) * det;
            data[6] = (+m.data[4] * m.data[2] - m.data[0] * m.data[6] * m.data[15]) * det;
            data[10] = (-m.data[4] * m.data[1] + m.data[0] * m.data[5] * m.data[15]) * det;
            data[3] = (m.data[9] * m.data[6] * m.data[3]
            - m.data[5] * m.data[10] * m.data[3]
            - m.data[9] * m.data[2] * m.data[7]
            + m.data[1] * m.data[10] * m.data[7]
            + m.data[5] * m.data[2] * m.data[11]
            - m.data[1] * m.data[6] * m.data[11]) * det;
            data[7] = (-m.data[8] * m.data[6] * m.data[3]
            + m.data[4] * m.data[10] * m.data[3]
            + m.data[8] * m.data[2] * m.data[7]
            - m.data[0] * m.data[10] * m.data[7]
            - m.data[4] * m.data[2] * m.data[11]
            + m.data[0] * m.data[6] * m.data[11]) * det;
            data[11] = (m.data[8] * m.data[5] * m.data[3]
            - m.data[4] * m.data[9] * m.data[3]
            - m.data[8] * m.data[1] * m.data[7]
            + m.data[0] * m.data[9] * m.data[7]
            + m.data[4] * m.data[1] * m.data[11]
            - m.data[0] * m.data[5] * m.data[11]) * det;
        }
        void setOrientationAndPos(Quaternion q, Vector3 pos)
        {
            data[0] = 1 - (2 * q.j * q.j + 2 * q.k * q.k);
            data[1] = 2 * q.i * q.j + 2 * q.k * q.r;
            data[2] = 2 * q.i * q.k - 2 * q.j * q.r;
            data[3] = pos.x;
            data[4] = 2 * q.i * q.j - 2 * q.k * q.r;
            data[5] = 1 - (2 * q.i * q.i + 2 * q.k * q.k);
            data[6] = 2 * q.j * q.k + 2 * q.i * q.r;
            data[7] = pos.y;
            data[8] = 2 * q.i * q.k + 2 * q.j * q.r;
            data[9] = 2 * q.j * q.k - 2 * q.i * q.r;
            data[10] = 1 - (2 * q.i * q.i + 2 * q.j * q.j);
            data[11] = pos.z;
        }
        Vector3 transformInverse(Vector3 vector)
        {
            Vector3 tmp = vector;
            tmp.x -= data[3];
            tmp.y -= data[7];
            tmp.z -= data[11];
            return new Vector3(
            tmp.x * data[0] +
            tmp.y * data[4] +
            tmp.z * data[8],
            tmp.x * data[1] +
            tmp.y * data[5] +
            tmp.z * data[9],
            tmp.x * data[2] +
            tmp.y * data[6] +
            tmp.z * data[10]
            );
        }
        Vector3 transformDirection(Vector3 vector)
        {
            return new Vector3(
            vector.x * data[0] +
            vector.y * data[1] +
            vector.z * data[2],
            vector.x * data[4] +
            vector.y * data[5] +
            vector.z * data[6],
            vector.x * data[8] +
            vector.y * data[9] +
            vector.z * data[10]
            );
        }

        Vector3 transformInverseDirection(Vector3 vector)
        {
            return new Vector3(
            vector.x * data[0] +
            vector.y * data[4] +
            vector.z * data[8],
            vector.x * data[1] +
            vector.y * data[5] +
            vector.z * data[9],
            vector.x * data[2] +
            vector.y * data[6] +
            vector.z * data[10]
            );
        }
        public Vector3 transform(Vector3 vector)
        {
            return this * vector;
        }
    }
    class Quaternion
    {
        public float i, j, k, r;
        public float[] data;
        Quaternion(float r, float i, float j, float k)
        {
            r = r;
            i = i;
            j = j;
            k = k;

            data = [r, i, j, k];
        }
        Quaternion()
        {
            r = 1;
            i = 0;
            j = 0;
            k = 0;
            data = [1f, 0f, 0f, 0f];
        }
        void normalise()
        {
            float d = r * r + i * i + j * j + k * k;

            // Check for zero length quaternion, and use the no-rotation
            // quaternion in that case.
            if (d < float.Epsilon)
            {
                r = 1;
                return;
            }

            d = (1f) / MathF.Sqrt(d);
            r *= d;
            i *= d;
            j *= d;
            k *= d;
        }
        public static Quaternion operator *(Quaternion q, Quaternion multiplier) =>
        new()
        {
            r = q.r * multiplier.r - q.i * multiplier.i -
                q.j * multiplier.j - q.k * multiplier.k,
            i = q.r * multiplier.i + q.i * multiplier.r +
                q.j * multiplier.k - q.k * multiplier.j,
            j = q.r * multiplier.j + q.j * multiplier.r +
                q.k * multiplier.i - q.i * multiplier.k,
            k = q.r * multiplier.k + q.k * multiplier.r +
                q.i * multiplier.j - q.j * multiplier.i,
        };
        void addScaledVector(Vector3 vector, float scale)
        {
            Quaternion q = new Quaternion(0,
                vector.x * scale,
                vector.y * scale,
                vector.z * scale);
            q *= this;
            r += q.r * 0.5f;
            i += q.i * 0.5f;
            j += q.j * 0.5f;
            k += q.k * 0.5f;
            data = [r, i, j, k];
        }
        void rotateByVector(Vector3 vector)
        {
            Quaternion q = new Quaternion(0, vector.x, vector.y,
            vector.z);
            Quaternion p = this;
            p *= q;
            r = p.r;
            j = p.j;
            k = p.k;
            i = p.i;
            data = [r, i, j, k];
        }
    }

}