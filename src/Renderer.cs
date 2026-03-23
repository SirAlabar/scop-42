using System;
using OpenTK.Graphics.OpenGL4;
using Scop.Math;
using Scop.Rendering;

namespace Scop
{
    // Builds MVP matrices, uploads uniforms, executes draw call.
    // No input logic. No state of its own.

    public static class Renderer
    {
        /* ── Constants ───────────────────────────────────────────────────── */

        private const float FovY      = MathF.PI / 4f;   // 45 degrees
        private const float NearPlane = 0.1f;
        private const float FarPlane  = 100f;

        /* ── Draw ────────────────────────────────────────────────────────── */

        public static void Draw(AppState state, int viewportWidth, int viewportHeight)
        {
            if (state.Shader == null || state.Mesh == null || state.Texture == null)
            {
                return;
            }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            state.Shader.Use();

            Mat4 model = BuildModel(state);
            Mat4 view  = BuildView(state);
            Mat4 proj  = BuildProjection(viewportWidth, viewportHeight);

            /* ── Blend — forced to 0 in wireframe mode for clean lines ──── */

            float blend = state.WireframeOn ? 0f : state.BlendFactor;

            state.Shader.SetMat4("u_model",        model);
            state.Shader.SetMat4("u_view",         view);
            state.Shader.SetMat4("u_projection",   proj);
            state.Shader.SetFloat("u_blendFactor", blend);
            state.Shader.SetInt("u_texture",        0);

            /* ── Backface culling — enabled only when toggled on ─────────── */

            if (state.CullingOn)
            {
                GL.Enable(EnableCap.CullFace);
                GL.CullFace(CullFaceMode.Back);
            }

            /* ── Wireframe — set polygon mode, draw, restore ─────────────── */

            if (state.WireframeOn)
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            }

            state.Texture.Bind(0);
            state.Mesh.Draw();
            state.Texture.Unbind();

            if (state.WireframeOn)
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }

            if (state.CullingOn)
            {
                GL.Disable(EnableCap.CullFace);
            }
        }

        /* ── Matrix builders ─────────────────────────────────────────────── */

        private static Mat4 BuildModel(AppState state)
        {
            return Mat4.Translate(state.Position)
                 * Mat4.RotateY(state.RotAngle + state.ManualRotY)
                 * Mat4.RotateX(state.ManualRotX);
        }

        private static Mat4 BuildView(AppState state)
        {
            return Mat4.LookAt(
                new Vec3(0f, 0f, state.CameraZ),
                Vec3.Zero,
                Vec3.UnitY
            );
        }

        private static Mat4 BuildProjection(int width, int height)
        {
            float aspect = (float)width / (float)height;
            return Mat4.Perspective(FovY, aspect, NearPlane, FarPlane);
        }
    }
}