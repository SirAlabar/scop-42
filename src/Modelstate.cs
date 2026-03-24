using Scop.Math;
using Scop.Rendering;
using Scop.Rendering.Interfaces;

namespace Scop
{
    // All state that belongs to one loaded model.
    // AppState holds two of these in dual mode.

    public class ModelState
    {
        /* ── Assets ──────────────────────────────────────────────────────── */

        public Mesh     Mesh    = null!;
        public ITexture Texture = null!;

        /* ── Transform ───────────────────────────────────────────────────── */

        public Vec3     Position   = Vec3.Zero;
        public float    RotAngle   = 0f;

        /* ── Manual rotation (LMB drag) ──────────────────────────────────── */

        public float    ManualRotX   = 0f;
        public float    ManualRotY   = 0f;
        public bool     IsDragging   = false;
        public Vec2     MouseLastPos;

        /* ── Texture blend ───────────────────────────────────────────────── */

        public float    BlendFactor = 0f;
        public float    BlendTarget = 0f;
        public bool     TextureOn   = false;

        /* ── Bonus 1 — Wireframe ─────────────────────────────────────────── */

        public bool     WireframeOn = false;

        /* ── Bonus 2 — Backface culling ──────────────────────────────────── */

        public bool     CullingOn   = false;

        /* ── Bonus 5 — Flat shading (true = flat, false = smooth) ────────── */

        public bool     FlatShading = true;

        /* ── Bonus 6 — Phong lighting ────────────────────────────────────── */

        public bool     LightOn         = true;
        public Vec3     LightColor      = Vec3.One;
        public float    AmbientStrength = 0.15f;
        public float    Shininess       = 32f;
    }
}