namespace Scop.Rendering.Interfaces
{
    // Null Object: contract implemented by both Texture (real) and NullTexture (no-op).
    // App.cs holds ITexture — never the concrete type.
    // This removes all null checks from the render loop.

    public interface ITexture
    {
        bool IsLoaded { get; }
        void Bind(int unit = 0);
        void Unbind();
    }
}