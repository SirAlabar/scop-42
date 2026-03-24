using Scop.Math;

namespace Scop.Parsing
{
    // One interleaved vertex uploaded to the VBO.
    //
    // Memory layout (stride = 44 bytes):
    //
    //   offset  0  →  Position  (Vec3 = 3 × float = 12 bytes)
    //   offset 12  →  Color     (Vec3 = 3 × float = 12 bytes)
    //   offset 24  →  TexCoord  (Vec2 = 2 × float =  8 bytes)
    //   offset 32  →  Normal    (Vec3 = 3 × float = 12 bytes)
    //                                          total = 44 bytes

    public struct Vertex
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        public Vec3 Position;
        public Vec3 Color;
        public Vec2 TexCoord;
        public Vec3 Normal;

        /* ── Constants ───────────────────────────────────────────────────── */

        public const int Stride = 44;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public Vertex(Vec3 position, Vec3 color, Vec2 texCoord, Vec3 normal)
        {
            Position = position;
            Color    = color;
            TexCoord = texCoord;
            Normal   = normal;
        }
    }
}