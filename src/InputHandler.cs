using System;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Scop.Math;

namespace Scop
{
    // Reads keyboard and mouse state, mutates AppState.
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
        private const float FovY        = MathF.PI / 4f;
        private const float Aspect      = 16f / 9f;

        /* ── Per-frame update ────────────────────────────────────────────── */

        public static void Update(AppState state, KeyboardState keyboard, float dt)
        {
            HandleTextureToggle(state, keyboard);
            HandleWireframeToggle(state, keyboard);
            HandleCullingToggle(state, keyboard);
            HandleScreenshotRequest(state, keyboard);
            HandleShadingToggle(state, keyboard);
            HandleLightToggle(state, keyboard);
            HandleTranslation(state, keyboard);
            UpdateRotation(state, dt);
            UpdateBlend(state, dt);
        }

        /* ── LMB events — object rotation ───────────────────────────────── */

        public static void OnMouseDown(AppState state, float mouseX, float mouseY)
        {
            state.IsDragging   = true;
            state.MouseLastPos = new Vec2(mouseX, mouseY);
        }

        public static void OnMouseMove(AppState state, float mouseX, float mouseY)
        {
            if (!state.IsDragging)
            {
                return;
            }

            Vec2  current = new Vec2(mouseX, mouseY);
            float dx      = current.X - state.MouseLastPos.X;
            float dy      = current.Y - state.MouseLastPos.Y;

            state.ManualRotY   += dx * DragSens;
            state.ManualRotX   += dy * DragSens;
            state.MouseLastPos  = current;
        }

        public static void OnMouseUp(AppState state)
        {
            state.IsDragging = false;
        }

        /* ── RMB events — light follows mouse directly ───────────────────── */

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

            /* ── Convert mouse pixel → NDC ───────────────────────────────── */

            float ndcX =  (mouseX / viewportWidth)  * 2f - 1f;
            float ndcY = -(mouseY / viewportHeight)  * 2f + 1f;   // flip Y

            /* ── Unproject NDC → world space at LightPos.Z depth ─────────── */

            float depth = state.CameraZ - state.LightPos.Z;
            float halfY = depth * MathF.Tan(FovY * 0.5f);
            float halfX = halfY * Aspect;

            state.LightPos.X = ndcX * halfX;
            state.LightPos.Y = ndcY * halfY;

            /* ── Clamp to frustum — prevent sphere leaving screen ────────── */

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
                ClampLightXY(state);  // re-clamp XY as depth changed
            }
            else
            {
                state.CameraZ -= offsetY * ZoomSpeed;
                state.CameraZ  = MathF.Max(ZoomMin, MathF.Min(ZoomMax, state.CameraZ));
            }
        }

        /* ── Private helpers ─────────────────────────────────────────────── */

        private static void HandleTextureToggle(AppState state, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.T))
            {
                state.TextureOn   = !state.TextureOn;
                state.BlendTarget = state.TextureOn ? 1.0f : 0.0f;
            }
        }

        private static void HandleWireframeToggle(AppState state, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.W))
            {
                state.WireframeOn = !state.WireframeOn;
            }
        }

        private static void HandleCullingToggle(AppState state, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.C))
            {
                state.CullingOn = !state.CullingOn;
            }
        }

        private static void HandleScreenshotRequest(AppState state, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.P))
            {
                state.ScreenshotRequested = true;
            }
        }

        private static void HandleShadingToggle(AppState state, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.S))
            {
                state.FlatShading = !state.FlatShading;
            }
        }

        private static void HandleLightToggle(AppState state, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.L))
            {
                state.LightOn = !state.LightOn;
            }
        }

        private static void HandleTranslation(AppState state, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Left))  { state.Position.X -= MoveStep; }
            if (keyboard.IsKeyDown(Keys.Right)) { state.Position.X += MoveStep; }
            if (keyboard.IsKeyDown(Keys.Up))    { state.Position.Y += MoveStep; }
            if (keyboard.IsKeyDown(Keys.Down))  { state.Position.Y -= MoveStep; }
            if (keyboard.IsKeyDown(Keys.Q))     { state.Position.Z += MoveStep; }
            if (keyboard.IsKeyDown(Keys.E))     { state.Position.Z -= MoveStep; }
        }

        private static void UpdateRotation(AppState state, float dt)
        {
            if (!state.IsDragging)
            {
                state.RotAngle += RotSpeed * dt;

                if (state.RotAngle > 2f * MathF.PI)
                {
                    state.RotAngle -= 2f * MathF.PI;
                }
            }
        }

        private static void UpdateBlend(AppState state, float dt)
        {
            if (state.BlendFactor < state.BlendTarget)
            {
                state.BlendFactor = MathF.Min(
                    state.BlendFactor + BlendSpeed * dt,
                    state.BlendTarget
                );
            }
            else if (state.BlendFactor > state.BlendTarget)
            {
                state.BlendFactor = MathF.Max(
                    state.BlendFactor - BlendSpeed * dt,
                    state.BlendTarget
                );
            }
        }

        /* ── Light clamping ──────────────────────────────────────────────── */

        private static void ClampLightZ(AppState state)
        {
            float lightZMax  = state.CameraZ - 0.2f;
            state.LightPos.Z = MathF.Max(0.2f, MathF.Min(lightZMax, state.LightPos.Z));
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