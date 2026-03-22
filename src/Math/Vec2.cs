namespace Scop.Math
{
    public struct Vec2
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        public float X;
        public float Y;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /* ── Static constants ────────────────────────────────────────────── */

        public static Vec2 Zero => new Vec2(0f, 0f);
        public static Vec2 One  => new Vec2(1f, 1f);

        /* ── Arithmetic operators ────────────────────────────────────────── */

        public static Vec2 operator +(Vec2 a, Vec2 b)
        {
            return new Vec2(a.X + b.X, a.Y + b.Y);
        }

        public static Vec2 operator -(Vec2 a, Vec2 b)
        {
            return new Vec2(a.X - b.X, a.Y - b.Y);
        }

        public static Vec2 operator *(Vec2 v, float scalar)
        {
            return new Vec2(v.X * scalar, v.Y * scalar);
        }

        public static Vec2 operator *(float scalar, Vec2 v)
        {
            return new Vec2(v.X * scalar, v.Y * scalar);
        }

        public static Vec2 operator /(Vec2 v, float scalar)
        {
            return new Vec2(v.X / scalar, v.Y / scalar);
        }

        /* ── Utility ─────────────────────────────────────────────────────── */

        public override string ToString()
        {
            return $"Vec2({X}, {Y})";
        }
    }
}