using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Scop.Parsing;
using Scop.Parsing.Triangulation;
using Scop.Parsing.UvMapping;
using Scop.Rendering;
using Scop.Rendering.Interfaces;

namespace Scop
{
    // Thin orchestrator — owns AppState, extends GameWindow.
    // Delegates input to InputHandler, rendering to Renderer.

    public class App : GameWindow
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        private readonly string     _objPath;
        private readonly string     _texPath;
        private          AppState   _state = null!;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public App(string objPath, string texPath)
            : base(
                new GameWindowSettings
                {
                    UpdateFrequency = 60.0
                },
                new NativeWindowSettings
                {
                    Title         = "SCOP - 42",
                    ClientSize    = new OpenTK.Mathematics.Vector2i(1280, 720),
                    API           = ContextAPI.OpenGL,
                    APIVersion    = new Version(4, 1),
                    Profile       = ContextProfile.Core,
                    IsEventDriven = false
                }
            )
        {
            _objPath = objPath;
            _texPath = texPath;
        }

        /* ── OnLoad ──────────────────────────────────────────────────────── */

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(0.08f, 0.08f, 0.10f, 1.0f);

            Console.WriteLine(
                $"App: OpenGL {GL.GetString(StringName.Version)}" +
                $" / GLSL {GL.GetString(StringName.ShadingLanguageVersion)}"
            );

            _state = new AppState();

            LoadShader();
            LoadMesh();
            LoadTexture();
        }

        /* ── OnUpdateFrame ───────────────────────────────────────────────── */

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
                return;
            }

            InputHandler.Update(_state, KeyboardState, (float)args.Time);
        }

        /* ── OnRenderFrame ───────────────────────────────────────────────── */

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            Renderer.Draw(_state, Size.X, Size.Y);

            /* ── Bonus 4 — screenshot captured before swap ───────────────── */

            if (_state.ScreenshotRequested)
            {
                // Use FramebufferSize — matches the actual GL pixel buffer,
                // not the window client area which may differ on some WMs.
                Screenshot.Capture(FramebufferSize.X, FramebufferSize.Y);
                _state.ScreenshotRequested = false;
            }

            SwapBuffers();
        }

        /* ── OnResize ────────────────────────────────────────────────────── */

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        /* ── Mouse events — forwarded to InputHandler ────────────────────── */

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButton.Left)
            {
                InputHandler.OnMouseDown(_state, MousePosition.X, MousePosition.Y);
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            InputHandler.OnMouseMove(_state, e.X, e.Y);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButton.Left)
            {
                InputHandler.OnMouseUp(_state);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            InputHandler.OnMouseWheel(_state, e.OffsetY);
        }

        /* ── OnUnload ────────────────────────────────────────────────────── */

        protected override void OnUnload()
        {
            base.OnUnload();

            _state.Mesh?.Dispose();
            _state.Shader?.Dispose();

            if (_state.Texture is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /* ── Load helpers ────────────────────────────────────────────────── */

        private void LoadShader()
        {
            _state.Shader = new Shader();

            if (!_state.Shader.Load("shaders/vertex.glsl", "shaders/fragment.glsl"))
            {
                throw new Exception("App: shader load failed");
            }
        }

        private void LoadMesh()
        {
            ObjMesh meshData = ObjParser.Parse(
                _objPath,
                new FanTriangulator(),
                new BoxUvMapper()
            );

            if (meshData.Vertices.Count == 0)
            {
                throw new Exception($"App: no geometry in '{_objPath}'");
            }

            _state.Mesh = new Mesh();
            _state.Mesh.Upload(meshData);
        }

        private void LoadTexture()
        {
            _state.Texture = Texture.FromFile(_texPath);

            if (!_state.Texture.IsLoaded)
            {
                Console.WriteLine("App: texture unavailable — colour-only mode");
            }
        }
    }
}