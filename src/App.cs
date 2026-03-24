using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Scop.Math;
using Scop.Parsing;
using Scop.Parsing.Interfaces;
using Scop.Parsing.Triangulation;
using Scop.Parsing.UvMapping;
using Scop.Rendering;

namespace Scop
{
    // Thin orchestrator — owns AppState, extends GameWindow.
    // Delegates input to InputHandler, rendering to Renderer.

    public class App : GameWindow
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        private readonly string   _objPathA;
        private readonly string   _texPathA;
        private readonly string   _objPathB;
        private readonly string   _texPathB;
        private readonly bool     _bonusMode;
        private          AppState _state = null!;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public App(
            string objPathA,
            string texPathA,
            string objPathB,
            string texPathB,
            bool   bonusMode)
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
            _objPathA  = objPathA;
            _texPathA  = texPathA;
            _objPathB  = objPathB;
            _texPathB  = texPathB;
            _bonusMode = bonusMode;
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

            if (_bonusMode)
            {
                Console.WriteLine("App: bonus mode — EarClipper + FaceNormalUvMapper");
            }
            else
            {
                Console.WriteLine("App: standard mode — FanTriangulator + BoxUvMapper");
            }

            _state = new AppState();

            LoadShader();
            LoadModel(0, _objPathA, _texPathA);

            if (!string.IsNullOrEmpty(_objPathB))
            {
                LoadModel(1, _objPathB, _texPathB);
                _state.DualMode = true;
                UpdateTitle();
            }

            LoadLightSphere();
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

            bool switchedModel = InputHandler.Update(
                _state, KeyboardState, (float)args.Time
            );

            if (switchedModel)
            {
                UpdateTitle();
            }
        }

        /* ── OnRenderFrame ───────────────────────────────────────────────── */

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            Renderer.Draw(_state, Size.X, Size.Y);

            if (_state.ScreenshotRequested)
            {
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

        /* ── Mouse events ────────────────────────────────────────────────── */

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButton.Left)
            {
                InputHandler.OnMouseDown(_state, MousePosition.X, MousePosition.Y);
            }
            else if (e.Button == MouseButton.Right)
            {
                InputHandler.OnRmbDown(_state);
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            InputHandler.OnMouseMove(_state, e.X, e.Y);
            InputHandler.OnRmbMove(_state, e.X, e.Y, Size.X, Size.Y);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButton.Left)
            {
                InputHandler.OnMouseUp(_state);
            }
            else if (e.Button == MouseButton.Right)
            {
                InputHandler.OnRmbUp(_state);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            bool rmbHeld = MouseState.IsButtonDown(MouseButton.Right);
            InputHandler.OnMouseWheel(_state, e.OffsetY, rmbHeld);
        }

        /* ── OnUnload ────────────────────────────────────────────────────── */

        protected override void OnUnload()
        {
            base.OnUnload();

            foreach (ModelState m in _state.Models)
            {
                m.Mesh?.Dispose();

                if (m.Texture is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _state.Shader?.Dispose();
            _state.LightSphere?.Dispose();
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

        private void LoadModel(int index, string objPath, string texPath)
        {
            /* ── Strategy selection based on --bonus flag ────────────────── */

            ITriangulator triangulator;
            IUvMapper     uvMapper;

            if (_bonusMode)
            {
                triangulator = new EarClipper();
                uvMapper     = new FaceNormalUvMapper();
            }
            else
            {
                triangulator = new FanTriangulator();
                uvMapper     = new BoxUvMapper();
            }

            ObjMesh meshData = ObjParser.Parse(objPath, triangulator, uvMapper);

            if (meshData.Vertices.Count == 0)
            {
                throw new Exception($"App: no geometry in '{objPath}'");
            }

            _state.Models[index].Mesh = new Mesh();
            _state.Models[index].Mesh.Upload(meshData);

            _state.Models[index].Texture = Texture.FromFile(texPath);

            if (!_state.Models[index].Texture.IsLoaded)
            {
                Console.WriteLine($"App: model {index} texture unavailable — colour-only mode");
            }
        }

        private void LoadLightSphere()
        {
            _state.LightSphere = new LightSphere();
            _state.LightSphere.Upload(Vec3.One);
        }

        /* ── UI helpers ──────────────────────────────────────────────────── */

        private void UpdateTitle()
        {
            if (_state.DualMode)
            {
                string label = _state.ActiveModel == 0 ? "A" : "B";
                Title = $"SCOP - 42  [{label}]";
            }
            else
            {
                Title = "SCOP - 42";
            }
        }
    }
}