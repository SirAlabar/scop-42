using Scop.Math;
using Scop.Rendering;
using Scop.Rendering.Interfaces;

namespace Scop
{
    // All runtime state in one place.
    // InputHandler and Renderer receive this — no global state, no circular deps.

    public class AppState
    {
        /* ── Assets ──────────────────────────────────────────────────────── */

        public Shader   Shader  = null!;
        public Mesh     Mesh    = null!;
        public ITexture Texture = null!;

        /* ── Transform ───────────────────────────────────────────────────── */

        public Vec3     Position    = Vec3.Zero;
        public float    RotAngle    = 0f;
        public float    CameraZ     = 5.0f;

        /* ── Manual rotation ─────────────────────────────────────────────── */

        public float    ManualRotX  = 0f;
        public float    ManualRotY  = 0f;
        public bool     IsDragging  = false;
        public Vec2     MouseLastPos;

        /* ── Texture blend ───────────────────────────────────────────────── */

        public float    BlendFactor = 0f;
        public float    BlendTarget = 0f;
        public bool     TextureOn   = false;

        /* ── Bonus 1 — Wireframe ─────────────────────────────────────────── */

        public bool     WireframeOn = false;

        /* ── Bonus 2 — Backface culling ──────────────────────────────────── */

        public bool     CullingOn   = false;

        /* ── Bonus 4 — Screenshot ────────────────────────────────────────── */

        public bool     ScreenshotRequested = false;
    }
}