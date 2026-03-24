using Scop.Math;
using Scop.Parsing.Interfaces;

namespace Scop.Parsing.UvMapping
{
    // Strategy — bonus UV mapping.
    // Detects the dominant axis of the face normal and projects
    // the vertex position onto the two remaining axes.
    //
    // Both U and V are normalized by the same uniform scale derived
    // from the largest axis of the object bounding box.
    // This keeps texel density consistent across all faces and prevents
    // stretching on thin faces (e.g. narrow side extrusions).
    //
    // Dominant axis    Project onto
    // X (side)         YZ plane    U = Z / uniformScale,  V = Y / uniformScale
    // Y (top/bottom)   XZ plane    U = X / uniformScale,  V = Z / uniformScale
    // Z (front/back)   XY plane    U = X / uniformScale,  V = Y / uniformScale

    public class FaceNormalUvMapper : IUvMapper
    {
        /* ── IUvMapper ───────────────────────────────────────────────────── */

        public Vec2 Map(Vec3 position, Vec3 faceNormal, Vec3 minBound, Vec3 maxBound)
        {
            /* ── Uniform scale from largest bounding box axis ────────────── */

            float extentX = maxBound.X - minBound.X;
            float extentY = maxBound.Y - minBound.Y;
            float extentZ = maxBound.Z - minBound.Z;

            float uniform = System.MathF.Max(extentX, System.MathF.Max(extentY, extentZ));

            if (uniform < 1e-6f)
            {
                uniform = 1f;
            }

            /* ── Dominant axis selection ─────────────────────────────────── */

            float absX = System.MathF.Abs(faceNormal.X);
            float absY = System.MathF.Abs(faceNormal.Y);
            float absZ = System.MathF.Abs(faceNormal.Z);

            float rawU;
            float rawV;
            float originU;
            float originV;

            if (absX >= absY && absX >= absZ)
            {
                /* ── X dominant — project onto YZ plane ──────────────────── */

                rawU    = position.Z;
                rawV    = position.Y;
                originU = minBound.Z;
                originV = minBound.Y;
            }
            else if (absY >= absX && absY >= absZ)
            {
                /* ── Y dominant — project onto XZ plane ──────────────────── */

                rawU    = position.X;
                rawV    = position.Z;
                originU = minBound.X;
                originV = minBound.Z;
            }
            else
            {
                /* ── Z dominant — project onto XY plane ──────────────────── */

                rawU    = position.X;
                rawV    = position.Y;
                originU = minBound.X;
                originV = minBound.Y;
            }

            /* ── Normalize both axes by the same uniform scale ───────────── */

            float u = (rawU - originU) / uniform;
            float v = (rawV - originV) / uniform;

            /* ── Flip V to match OpenGL bottom-left origin ───────────────── */

            return new Vec2(u, 1f - v);
        }
    }
}