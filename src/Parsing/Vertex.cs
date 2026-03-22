using Scop.Math;

namespace Scop.Parsing
{
    // One interleaved vertex uploaded to the VBO.
    //
    // Memory layout (stride = 32 bytes):
    //
    //   offset  0  →  Position  (Vec3 = 3 × float = 12 bytes)
    //   offset 12  →  Color     (Vec3 = 3 × float = 12 bytes)
    //   offset 24  →  TexCoord  (Vec2 = 2 × float =  8 bytes)
    //                                          total = 32 bytes

    public struct Vertex
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        public Vec3 Position;
        public Vec3 Color;
        public Vec2 TexCoord;

        /* ── Constants ───────────────────────────────────────────────────── */

        public const int Stride = 32;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public Vertex(Vec3 position, Vec3 color, Vec2 texCoord)
        {
            Position = position;
            Color    = color;
            TexCoord = texCoord;
        }
    }
}