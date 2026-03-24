using Scop.Math;
using Scop.Rendering;

namespace Scop
{
    // All runtime state in one place.
    // InputHandler and Renderer receive this — no global state, no circular deps.

    public class AppState
    {
        /* ── Shared assets ───────────────────────────────────────────────── */

        public Shader      Shader      = null!;
        public LightSphere LightSphere = null!;

        /* ── Per-model state ─────────────────────────────────────────────── */

        public ModelState[] Models     = new ModelState[2]
        {
            new ModelState(),
            new ModelState()
        };

        /* ── Dual mode ───────────────────────────────────────────────────── */

        public bool     DualMode    = false;
        public int      ActiveModel = 0;    // 0 = left, 1 = right

        /* ── Shared camera ───────────────────────────────────────────────── */

        public float    CameraZ     = 5.0f;

        /* ── Shared light — one light source affects both models ─────────── */

        public Vec3     LightPos        = new Vec3(0.5f, 0.5f, 3f);
        public bool     IsRmbDragging   = false;

        /* ── Bonus 4 — Screenshot ────────────────────────────────────────── */

        public bool     ScreenshotRequested = false;
    }
}