using Scop.Rendering.Interfaces;

namespace Scop.Rendering
{
    // Null Object — returned by Texture.FromFile() when loading fails.
    // Every method is a silent no-op.
    // App.cs never needs to check if the texture loaded — it just calls Bind/Unbind.

    public class NullTexture : ITexture
    {
        /* ── ITexture ────────────────────────────────────────────────────── */

        public bool IsLoaded => false;

        public void Bind(int unit = 0)
        {
            // No-op — no texture to bind
        }

        public void Unbind()
        {
            // No-op — nothing was bound
        }
    }
}