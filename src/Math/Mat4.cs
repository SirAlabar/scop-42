using System;

namespace Scop.Math
{
    // Column-major storage to match OpenGL convention.
    // Memory layout: m[col * 4 + row]
    //
    // Mrc  =  row r, column c
    //
    // ┌                         ┐
    // │  M00  M01  M02  M03     │
    // │  M10  M11  M12  M13     │
    // │  M20  M21  M22  M23     │
    // │  M30  M31  M32  M33     │
    // └                         ┘

    public struct Mat4
    {
        /* ── Fields (column-major) ───────────────────────────────────────── */

        public float M00, M10, M20, M30;    // column 0
        public float M01, M11, M21, M31;    // column 1
        public float M02, M12, M22, M32;    // column 2
        public float M03, M13, M23, M33;    // column 3

        /* ── Identity ────────────────────────────────────────────────────── */

        public static Mat4 Identity()
        {
            Mat4 r = default;
            r.M00 = 1f;
            r.M11 = 1f;
            r.M22 = 1f;
            r.M33 = 1f;
            return r;
        }

        /* ── Translation ─────────────────────────────────────────────────── */

        public static Mat4 Translate(Vec3 t)
        {
            Mat4 r = Identity();
            r.M03 = t.X;
            r.M13 = t.Y;
            r.M23 = t.Z;
            return r;
        }

        /* ── Scale ───────────────────────────────────────────────────────── */

        public static Mat4 Scale(Vec3 s)
        {
            Mat4 r = Identity();
            r.M00 = s.X;
            r.M11 = s.Y;
            r.M22 = s.Z;
            return r;
        }

        /* ── Rotations ───────────────────────────────────────────────────── */

        // Rotation around X axis:
        // ┌  1    0     0    0 ┐
        // │  0    cos  -sin  0 │
        // │  0    sin   cos  0 │
        // └  0    0     0    1 ┘
        public static Mat4 RotateX(float rad)
        {
            Mat4    r = Identity();
            float   c = MathF.Cos(rad);
            float   s = MathF.Sin(rad);

            r.M11 =  c;
            r.M12 = -s;
            r.M21 =  s;
            r.M22 =  c;
            return r;
        }

        // Rotation around Y axis:
        // ┌  cos   0    sin   0 ┐
        // │  0     1    0     0 │
        // │ -sin   0    cos   0 │
        // └  0     0    0     1 ┘
        public static Mat4 RotateY(float rad)
        {
            Mat4    r = Identity();
            float   c = MathF.Cos(rad);
            float   s = MathF.Sin(rad);

            r.M00 =  c;
            r.M02 =  s;
            r.M20 = -s;
            r.M22 =  c;
            return r;
        }

        // Rotation around Z axis:
        // ┌  cos  -sin   0    0 ┐
        // │  sin   cos   0    0 │
        // │  0     0     1    0 │
        // └  0     0     0    1 ┘
        public static Mat4 RotateZ(float rad)
        {
            Mat4    r = Identity();
            float   c = MathF.Cos(rad);
            float   s = MathF.Sin(rad);

            r.M00 =  c;
            r.M01 = -s;
            r.M10 =  s;
            r.M11 =  c;
            return r;
        }

        /* ── Perspective projection ──────────────────────────────────────── */

        // fovY   : vertical field of view in radians (e.g. 45° = MathF.PI / 4f)
        // aspect : viewport width / height
        // near   : near clip plane distance (e.g. 0.1f)
        // far    : far clip plane distance  (e.g. 100f)
        public static Mat4 Perspective(float fovY, float aspect, float near, float far)
        {
            Mat4    r = default;
            float   f = 1f / MathF.Tan(fovY * 0.5f);

            r.M00 = f / aspect;
            r.M11 = f;
            r.M22 = (near + far) / (near - far);
            r.M32 = -1f;
            r.M23 = (2f * near * far) / (near - far);
            return r;
        }

        /* ── View (LookAt) ───────────────────────────────────────────────── */

        // Builds a view matrix that positions the camera at eye,
        // looking toward center, with up as the world up direction.
        //
        // f = forward  (where the camera looks)
        // r = right    (cross of forward and up)
        // u = real up  (cross of right and forward)
        public static Mat4 LookAt(Vec3 eye, Vec3 center, Vec3 up)
        {
            Vec3 f = (center - eye).Normalized();
            Vec3 r = Vec3.Cross(f, up).Normalized();
            Vec3 u = Vec3.Cross(r, f);

            Mat4 result = Identity();

            result.M00 =  r.X;  result.M01 =  r.Y;  result.M02 =  r.Z;
            result.M10 =  u.X;  result.M11 =  u.Y;  result.M12 =  u.Z;
            result.M20 = -f.X;  result.M21 = -f.Y;  result.M22 = -f.Z;

            result.M03 = -Vec3.Dot(r, eye);
            result.M13 = -Vec3.Dot(u, eye);
            result.M23 =  Vec3.Dot(f, eye);

            return result;
        }

        /* ── Matrix multiplication ───────────────────────────────────────── */

        public static Mat4 operator *(Mat4 a, Mat4 b)
        {
            Mat4 r = default;

            r.M00 = a.M00*b.M00 + a.M01*b.M10 + a.M02*b.M20 + a.M03*b.M30;
            r.M01 = a.M00*b.M01 + a.M01*b.M11 + a.M02*b.M21 + a.M03*b.M31;
            r.M02 = a.M00*b.M02 + a.M01*b.M12 + a.M02*b.M22 + a.M03*b.M32;
            r.M03 = a.M00*b.M03 + a.M01*b.M13 + a.M02*b.M23 + a.M03*b.M33;

            r.M10 = a.M10*b.M00 + a.M11*b.M10 + a.M12*b.M20 + a.M13*b.M30;
            r.M11 = a.M10*b.M01 + a.M11*b.M11 + a.M12*b.M21 + a.M13*b.M31;
            r.M12 = a.M10*b.M02 + a.M11*b.M12 + a.M12*b.M22 + a.M13*b.M32;
            r.M13 = a.M10*b.M03 + a.M11*b.M13 + a.M12*b.M23 + a.M13*b.M33;

            r.M20 = a.M20*b.M00 + a.M21*b.M10 + a.M22*b.M20 + a.M23*b.M30;
            r.M21 = a.M20*b.M01 + a.M21*b.M11 + a.M22*b.M21 + a.M23*b.M31;
            r.M22 = a.M20*b.M02 + a.M21*b.M12 + a.M22*b.M22 + a.M23*b.M32;
            r.M23 = a.M20*b.M03 + a.M21*b.M13 + a.M22*b.M23 + a.M23*b.M33;

            r.M30 = a.M30*b.M00 + a.M31*b.M10 + a.M32*b.M20 + a.M33*b.M30;
            r.M31 = a.M30*b.M01 + a.M31*b.M11 + a.M32*b.M21 + a.M33*b.M31;
            r.M32 = a.M30*b.M02 + a.M31*b.M12 + a.M32*b.M22 + a.M33*b.M32;
            r.M33 = a.M30*b.M03 + a.M31*b.M13 + a.M32*b.M23 + a.M33*b.M33;

            return r;
        }

        /* ── Matrix * Vec4 ───────────────────────────────────────────────── */

        public static Vec4 operator *(Mat4 m, Vec4 v)
        {
            return new Vec4(
                m.M00*v.X + m.M01*v.Y + m.M02*v.Z + m.M03*v.W,
                m.M10*v.X + m.M11*v.Y + m.M12*v.Z + m.M13*v.W,
                m.M20*v.X + m.M21*v.Y + m.M22*v.Z + m.M23*v.W,
                m.M30*v.X + m.M31*v.Y + m.M32*v.Z + m.M33*v.W
            );
        }

        /* ── Export for OpenGL ───────────────────────────────────────────── */

        // Returns a float[16] in column-major order for GL.UniformMatrix4
        public float[] ToArray()
        {
            return new float[]
            {
                M00, M10, M20, M30,     // column 0
                M01, M11, M21, M31,     // column 1
                M02, M12, M22, M32,     // column 2
                M03, M13, M23, M33      // column 3
            };
        }

        /* ── Utility ─────────────────────────────────────────────────────── */

        public override string ToString()
        {
            return $"Mat4(\n" +
                   $"  {M00} {M01} {M02} {M03}\n" +
                   $"  {M10} {M11} {M12} {M13}\n" +
                   $"  {M20} {M21} {M22} {M23}\n" +
                   $"  {M30} {M31} {M32} {M33}\n" +
                   $")";
        }
    }
}