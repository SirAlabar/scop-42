using System;
using OpenTK.Graphics.OpenGL4;
using Scop.Math;
using Scop.Rendering;

namespace Scop
{
    // Builds MVP matrices, uploads uniforms, executes draw calls.
    // No input logic. No state of its own.

    public static class Renderer
    {
        /* ── Constants ───────────────────────────────────────────────────── */

        private const float FovY      = MathF.PI / 4f;
        private const float NearPlane = 0.1f;
        private const float FarPlane  = 100f;

        /* ── Draw ────────────────────────────────────────────────────────── */

        public static void Draw(AppState state, int viewportWidth, int viewportHeight)
        {
            if (state.Shader == null)
            {
                return;
            }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (state.DualMode)
            {
                DrawDual(state, viewportWidth, viewportHeight);
            }
            else
            {
                DrawSingle(state, viewportWidth, viewportHeight);
            }
        }

        /* ── Single mode ─────────────────────────────────────────────────── */

        private static void DrawSingle(AppState state, int width, int height)
        {
            GL.Viewport(0, 0, width, height);

            Mat4 view = BuildView(state);
            Mat4 proj = BuildProjection(width, height);

            DrawModel(state, state.Models[0], view, proj);

            if (state.IsRmbDragging && state.LightSphere != null)
            {
                DrawLightSphere(state, view, proj);
            }
        }

        /* ── Dual mode ───────────────────────────────────────────────────── */

        private static void DrawDual(AppState state, int width, int height)
        {
            int halfW = width / 2;

            /* ── Left viewport — model A ─────────────────────────────────── */

            GL.Viewport(0, 0, halfW, height);

            Mat4 viewA = BuildView(state);
            Mat4 projA = BuildProjection(halfW, height);

            DrawModel(state, state.Models[0], viewA, projA);

            if (state.IsRmbDragging && state.LightSphere != null)
            {
                DrawLightSphere(state, viewA, projA);
            }

            /* ── Right viewport — model B ────────────────────────────────── */

            GL.Viewport(halfW, 0, halfW, height);

            Mat4 viewB = BuildView(state);
            Mat4 projB = BuildProjection(halfW, height);

            DrawModel(state, state.Models[1], viewB, projB);

            if (state.IsRmbDragging && state.LightSphere != null)
            {
                DrawLightSphere(state, viewB, projB);
            }

            /* ── Restore full viewport for screenshot / UI ───────────────── */

            GL.Viewport(0, 0, width, height);
        }

        /* ── Draw one model ──────────────────────────────────────────────── */

        private static void DrawModel(
            AppState  state,
            ModelState m,
            Mat4      view,
            Mat4      proj)
        {
            if (m.Mesh == null || m.Texture == null)
            {
                return;
            }

            state.Shader.Use();

            Mat4  model = BuildModelMatrix(m);
            float blend = m.WireframeOn ? 0f : m.BlendFactor;
            Vec3  eye   = new Vec3(0f, 0f, state.CameraZ);

            state.Shader.SetMat4("u_model",            model);
            state.Shader.SetMat4("u_view",             view);
            state.Shader.SetMat4("u_projection",       proj);
            state.Shader.SetFloat("u_blendFactor",     blend);
            state.Shader.SetInt("u_texture",            0);
            state.Shader.SetInt("u_flatShading",        m.FlatShading ? 1 : 0);
            state.Shader.SetInt("u_lightOn",            m.LightOn ? 1 : 0);
            state.Shader.SetVec3("u_lightPos",          state.LightPos);
            state.Shader.SetVec3("u_lightColor",        m.LightColor);
            state.Shader.SetFloat("u_ambientStrength",  m.AmbientStrength);
            state.Shader.SetFloat("u_shininess",        m.Shininess);
            state.Shader.SetVec3("u_viewPos",           eye);

            if (m.CullingOn)
            {
                GL.Enable(EnableCap.CullFace);
                GL.CullFace(CullFaceMode.Back);
            }

            if (m.WireframeOn)
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            }

            m.Texture.Bind(0);
            m.Mesh.Draw();
            m.Texture.Unbind();

            if (m.WireframeOn)
            {
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            }

            if (m.CullingOn)
            {
                GL.Disable(EnableCap.CullFace);
            }
        }

        /* ── Draw light sphere ───────────────────────────────────────────── */

        private static void DrawLightSphere(AppState state, Mat4 view, Mat4 proj)
        {
            state.Shader.Use();

            Vec3 eye   = new Vec3(0f, 0f, state.CameraZ);
            Mat4 model = Mat4.Translate(state.LightPos);

            state.Shader.SetMat4("u_model",            model);
            state.Shader.SetMat4("u_view",             view);
            state.Shader.SetMat4("u_projection",       proj);
            state.Shader.SetFloat("u_blendFactor",     0f);
            state.Shader.SetInt("u_texture",            0);
            state.Shader.SetInt("u_flatShading",        0);
            state.Shader.SetInt("u_lightOn",            1);
            state.Shader.SetVec3("u_lightPos",          eye);
            state.Shader.SetVec3("u_lightColor",        Vec3.One);
            state.Shader.SetFloat("u_ambientStrength",  0.3f);
            state.Shader.SetFloat("u_shininess",        64f);
            state.Shader.SetVec3("u_viewPos",           eye);

            state.LightSphere.Draw();
        }

        /* ── Matrix builders ─────────────────────────────────────────────── */

        private static Mat4 BuildModelMatrix(ModelState m)
        {
            return Mat4.Translate(m.Position)
                 * Mat4.RotateY(m.RotAngle + m.ManualRotY)
                 * Mat4.RotateX(m.ManualRotX);
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