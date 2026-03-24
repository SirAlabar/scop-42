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

            Mat4 view = BuildView(state);
            Mat4 proj = BuildProjection(viewportWidth, viewportHeight);

            /* ── Pass 1 — main mesh ──────────────────────────────────────── */

            DrawMesh(state, view, proj);

            /* ── Pass 2 — light sphere (only while RMB is held) ─────────── */

            if (state.IsRmbDragging && state.LightSphere != null)
            {
                DrawLightSphere(state, view, proj);
            }
        }

        /* ── Pass 1 ──────────────────────────────────────────────────────── */

        private static void DrawMesh(AppState state, Mat4 view, Mat4 proj)
        {
            state.Shader.Use();

            Mat4  model = BuildModel(state);
            float blend = state.WireframeOn ? 0f : state.BlendFactor;
            Vec3  eye   = new Vec3(0f, 0f, state.CameraZ);

            state.Shader.SetMat4("u_model",            model);
            state.Shader.SetMat4("u_view",             view);
            state.Shader.SetMat4("u_projection",       proj);
            state.Shader.SetFloat("u_blendFactor",     blend);
            state.Shader.SetInt("u_texture",            0);
            state.Shader.SetInt("u_flatShading",        state.FlatShading ? 1 : 0);
            state.Shader.SetInt("u_lightOn",            state.LightOn ? 1 : 0);
            state.Shader.SetVec3("u_lightPos",          state.LightPos);
            state.Shader.SetVec3("u_lightColor",        state.LightColor);
            state.Shader.SetFloat("u_ambientStrength",  state.AmbientStrength);
            state.Shader.SetFloat("u_shininess",        state.Shininess);
            state.Shader.SetVec3("u_viewPos",           eye);

            if (state.CullingOn)
            {
                GL.Enable(EnableCap.CullFace);
                GL.CullFace(CullFaceMode.Back);
            }

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

        /* ── Pass 2 ──────────────────────────────────────────────────────── */

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
            state.Shader.SetInt("u_flatShading",        0);   // smooth — looks rounder

            /* ── Light the sphere from the camera so it always looks 3D ─── */
            /* ── Using camera position as light avoids self-shadowing      ─ */

            state.Shader.SetInt("u_lightOn",            1);
            state.Shader.SetVec3("u_lightPos",          eye);
            state.Shader.SetVec3("u_lightColor",        state.LightColor);
            state.Shader.SetFloat("u_ambientStrength",  0.3f);
            state.Shader.SetFloat("u_shininess",        64f);
            state.Shader.SetVec3("u_viewPos",           eye);

            state.LightSphere.Draw();
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