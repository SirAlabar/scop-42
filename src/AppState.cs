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

        public Shader      Shader      = null!;
        public Mesh        Mesh        = null!;
        public ITexture    Texture     = null!;
        public LightSphere LightSphere = null!;

        /* ── Transform ───────────────────────────────────────────────────── */

        public Vec3     Position    = Vec3.Zero;
        public float    RotAngle    = 0f;
        public float    CameraZ     = 5.0f;

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

        /* ── Bonus 4 — Screenshot ────────────────────────────────────────── */

        public bool     ScreenshotRequested = false;

        /* ── Bonus 5 — Flat shading (true = flat, false = smooth) ────────── */

        public bool     FlatShading = true;

        /* ── Bonus 6 — Phong lighting ────────────────────────────────────── */

        public bool     LightOn         = true;

        // Initial position is within frustum bounds at Z=3, CameraZ=5:
        // depth=2, halfY=2*tan(PI/8)=0.83, halfX=halfY*(16/9)=1.47
        public Vec3     LightPos        = new Vec3(0.5f, 0.5f, 3f);
        public Vec3     LightColor      = Vec3.One;
        public float    AmbientStrength = 0.15f;
        public float    Shininess       = 32f;

        /* ── Bonus 6 — RMB drag (move light) ────────────────────────────── */

        public bool     IsRmbDragging  = false;
    }
}