using System;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Scop.Math;

namespace Scop
{
    // Reads keyboard and mouse state, mutates AppState.
    // All per-model input is routed to the active ModelState.
    // No OpenGL calls. No rendering logic.

    public static class InputHandler
    {
        /* ── Constants ───────────────────────────────────────────────────── */

        private const float MoveStep    = 0.05f;
        private const float ZoomSpeed   = 0.3f;
        private const float ZoomMin     = 0.5f;
        private const float ZoomMax     = 50f;
        private const float DragSens    = 0.005f;   // rad/pixel — LMB rotation
        private const float LightDragZ  = 0.3f;     // units/tick — scroll light Z
        private const float BlendSpeed  = 2.0f;
        private const float RotSpeed    = 1.0f;     // rad/s

        // Matches Renderer constants
        private const float FovY   = MathF.PI / 4f;
        private const float Aspect = 16f / 9f;

        /* ── Per-frame update ────────────────────────────────────────────── */

        // Returns true if the active model was switched this frame.
        public static bool Update(AppState state, KeyboardState keyboard, float dt)
        {
            ModelState active = state.Models[state.ActiveModel];

            bool switched = HandleModelSwitch(state, keyboard);

            HandleTextureToggle(active, keyboard);
            HandleWireframeToggle(active, keyboard);
            HandleCullingToggle(active, keyboard);
            HandleScreenshotRequest(state, keyboard);
            HandleShadingToggle(active, keyboard);
            HandleLightToggle(active, keyboard);
            HandleTranslation(active, keyboard);

            foreach (ModelState m in state.Models)
            {
                if (m.Mesh != null)
                {
                    UpdateRotation(m, dt);
                    UpdateBlend(m, dt);
                }
            }

            return switched;
        }

        /* ── LMB events — rotate active model ───────────────────────────── */

        public static void OnMouseDown(AppState state, float mouseX, float mouseY)
        {
            ModelState active = state.Models[state.ActiveModel];
            active.IsDragging   = true;
            active.MouseLastPos = new Vec2(mouseX, mouseY);
        }

        public static void OnMouseMove(AppState state, float mouseX, float mouseY)
        {
            ModelState active = state.Models[state.ActiveModel];

            if (!active.IsDragging)
            {
                return;
            }

            Vec2  current = new Vec2(mouseX, mouseY);
            float dx      = current.X - active.MouseLastPos.X;
            float dy      = current.Y - active.MouseLastPos.Y;

            active.ManualRotY   += dx * DragSens;
            active.ManualRotX   += dy * DragSens;
            active.MouseLastPos  = current;
        }

        public static void OnMouseUp(AppState state)
        {
            state.Models[state.ActiveModel].IsDragging = false;
        }

        /* ── RMB events — move light ─────────────────────────────────────── */

        public static void OnRmbDown(AppState state)
        {
            state.IsRmbDragging = true;
        }

        public static void OnRmbMove(
            AppState state,
            float    mouseX,
            float    mouseY,
            int      viewportWidth,
            int      viewportHeight)
        {
            if (!state.IsRmbDragging)
            {
                return;
            }

            /* ── Convert mouse pixel to NDC ──────────────────────────────── */

            float ndcX =  (mouseX / viewportWidth)  * 2f - 1f;
            float ndcY = -(mouseY / viewportHeight)  * 2f + 1f;

            /* ── Unproject to world space at LightPos.Z depth ────────────── */

            float depth = state.CameraZ - state.LightPos.Z;
            float halfY = depth * MathF.Tan(FovY * 0.5f);
            float halfX = halfY * Aspect;

            state.LightPos.X = ndcX * halfX;
            state.LightPos.Y = ndcY * halfY;

            ClampLightXY(state);
        }

        public static void OnRmbUp(AppState state)
        {
            state.IsRmbDragging = false;
        }

        /* ── Scroll — zoom or light Z ────────────────────────────────────── */

        public static void OnMouseWheel(AppState state, float offsetY, bool rmbHeld)
        {
            if (rmbHeld)
            {
                state.LightPos.Z -= offsetY * LightDragZ;
                ClampLightZ(state);
                ClampLightXY(state);
            }
            else
            {
                state.CameraZ -= offsetY * ZoomSpeed;
                state.CameraZ  = MathF.Max(ZoomMin, MathF.Min(ZoomMax, state.CameraZ));
            }
        }

        /* ── Private helpers ─────────────────────────────────────────────── */

        // Returns true if model was switched.
        private static bool HandleModelSwitch(AppState state, KeyboardState keyboard)
        {
            if (state.DualMode && keyboard.IsKeyPressed(Keys.Tab))
            {
                state.ActiveModel = state.ActiveModel == 0 ? 1 : 0;
                return true;
            }

            return false;
        }

        private static void HandleTextureToggle(ModelState m, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.T))
            {
                m.TextureOn   = !m.TextureOn;
                m.BlendTarget = m.TextureOn ? 1.0f : 0.0f;
            }
        }

        private static void HandleWireframeToggle(ModelState m, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.W))
            {
                m.WireframeOn = !m.WireframeOn;
            }
        }

        private static void HandleCullingToggle(ModelState m, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.C))
            {
                m.CullingOn = !m.CullingOn;
            }
        }

        private static void HandleScreenshotRequest(AppState state, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.P))
            {
                state.ScreenshotRequested = true;
            }
        }

        private static void HandleShadingToggle(ModelState m, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.S))
            {
                m.FlatShading = !m.FlatShading;
            }
        }

        private static void HandleLightToggle(ModelState m, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.L))
            {
                m.LightOn = !m.LightOn;
            }
        }

        private static void HandleTranslation(ModelState m, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Left))  { m.Position.X -= MoveStep; }
            if (keyboard.IsKeyDown(Keys.Right)) { m.Position.X += MoveStep; }
            if (keyboard.IsKeyDown(Keys.Up))    { m.Position.Y += MoveStep; }
            if (keyboard.IsKeyDown(Keys.Down))  { m.Position.Y -= MoveStep; }
            if (keyboard.IsKeyDown(Keys.Q))     { m.Position.Z += MoveStep; }
            if (keyboard.IsKeyDown(Keys.E))     { m.Position.Z -= MoveStep; }
        }

        private static void UpdateRotation(ModelState m, float dt)
        {
            if (!m.IsDragging)
            {
                m.RotAngle += RotSpeed * dt;

                if (m.RotAngle > 2f * MathF.PI)
                {
                    m.RotAngle -= 2f * MathF.PI;
                }
            }
        }

        private static void UpdateBlend(ModelState m, float dt)
        {
            if (m.BlendFactor < m.BlendTarget)
            {
                m.BlendFactor = MathF.Min(
                    m.BlendFactor + BlendSpeed * dt,
                    m.BlendTarget
                );
            }
            else if (m.BlendFactor > m.BlendTarget)
            {
                m.BlendFactor = MathF.Max(
                    m.BlendFactor - BlendSpeed * dt,
                    m.BlendTarget
                );
            }
        }

        /* ── Light clamping ──────────────────────────────────────────────── */

        private static void ClampLightZ(AppState state)
        {
            float lightZMax      = state.CameraZ - 0.2f;
            state.LightPos.Z     = MathF.Max(0.2f, MathF.Min(lightZMax, state.LightPos.Z));
        }

        private static void ClampLightXY(AppState state)
        {
            float depth  = state.CameraZ - state.LightPos.Z;
            float halfY  = depth * MathF.Tan(FovY * 0.5f);
            float halfX  = halfY * Aspect;

            state.LightPos.X = MathF.Max(-halfX, MathF.Min(halfX, state.LightPos.X));
            state.LightPos.Y = MathF.Max(-halfY, MathF.Min(halfY, state.LightPos.Y));
        }
    }
}