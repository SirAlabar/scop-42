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

        private const float MoveStep   = 0.05f;
        private const float ZoomSpeed  = 0.3f;
        private const float ZoomMin    = 0.5f;
        private const float ZoomMax    = 50f;
        private const float DragSens   = 0.005f;   // rad/pixel
        private const float BlendSpeed = 2.0f;
        private const float RotSpeed   = 1.0f;     // rad/s

        /* ── Per-frame update ────────────────────────────────────────────── */

        public static void Update(AppState state, KeyboardState keyboard, float dt)
        {
            HandleQuit(state, keyboard);
            HandleTextureToggle(state, keyboard);
            HandleTranslation(state, keyboard);
            UpdateRotation(state, dt);
            UpdateBlend(state, dt);
        }

        /* ── Mouse events ────────────────────────────────────────────────── */

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

        public static void OnMouseWheel(AppState state, float offsetY)
        {
            state.CameraZ -= offsetY * ZoomSpeed;
            state.CameraZ  = MathF.Max(ZoomMin, MathF.Min(ZoomMax, state.CameraZ));
        }

        /* ── Private helpers ─────────────────────────────────────────────── */

        private static void HandleQuit(AppState state, KeyboardState keyboard)
        {
            // Quit signal is read by App.cs via KeyboardState directly
            // Nothing to mutate here — App checks Escape itself in OnUpdateFrame
        }

        private static void HandleTextureToggle(AppState state, KeyboardState keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.T))
            {
                state.TextureOn   = !state.TextureOn;
                state.BlendTarget = state.TextureOn ? 1.0f : 0.0f;
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
    }
}