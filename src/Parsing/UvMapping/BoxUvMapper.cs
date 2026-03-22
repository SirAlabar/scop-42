using Scop.Math;
using Scop.Parsing.Interfaces;

namespace Scop.Parsing.UvMapping
{
    // Strategy — mandatory UV mapping.
    // Projects position onto the XY plane globally across the whole object.
    // Simple and fast — may produce stretching on side faces.
    // Replaced by FaceNormalUvMapper for bonus 2.

    public class BoxUvMapper : IUvMapper
    {
        /* ── IUvMapper ───────────────────────────────────────────────────── */

        public Vec2 Map(Vec3 position, Vec3 faceNormal, Vec3 minBound, Vec3 maxBound)
        {
            Vec3  range = maxBound - minBound;
            float u     = range.X > 1e-6f ? (position.X - minBound.X) / range.X : 0f;
            float v     = range.Y > 1e-6f ? (position.Y - minBound.Y) / range.Y : 0f;
            return new Vec2(u, v);
        }
    }
}