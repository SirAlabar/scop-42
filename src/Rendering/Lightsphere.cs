using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Scop.Math;
using Scop.Parsing;

namespace Scop.Rendering
{
    // Procedurally generated UV sphere used to visualise the light source.
    // Shares the same stride-44 vertex layout as Mesh so it reuses the
    // existing shader.  Rendered unlit (u_lightOn = 0) in the light colour.

    public class LightSphere : IDisposable
    {
        /* ── Constants ───────────────────────────────────────────────────── */

        private const int   Stacks = 12;
        private const int   Slices = 12;
        public  const float Radius = 0.05f;

        /* ── Fields ──────────────────────────────────────────────────────── */

        private int     _vao;
        private int     _vbo;
        private int     _ebo;
        private int     _indexCount;
        private bool    _disposed;

        /* ── Upload ──────────────────────────────────────────────────────── */

        public void Upload(Vec3 color)
        {
            List<float> verts   = new List<float>();
            List<uint>  indices = new List<uint>();

            BuildSphere(color, verts, indices);

            _indexCount = indices.Count;

            float[] vertexData = verts.ToArray();
            uint[]  indexData  = indices.ToArray();

            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);

            /* ── VBO ─────────────────────────────────────────────────────── */

            _vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                vertexData.Length * sizeof(float),
                vertexData,
                BufferUsageHint.StaticDraw
            );

            /* ── EBO ─────────────────────────────────────────────────────── */

            _ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                indexData.Length * sizeof(uint),
                indexData,
                BufferUsageHint.StaticDraw
            );

            /* ── Vertex attribute layout (stride = 44, same as Mesh) ─────── */

            int stride = Vertex.Stride;

            // location 0 — position
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

            // location 1 — color
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 12);

            // location 2 — texCoord (unused but must exist for stride)
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 24);

            // location 3 — normal (unused for unlit draw, but must exist for stride)
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, stride, 32);

            GL.BindVertexArray(0);
        }

        /* ── Draw ────────────────────────────────────────────────────────── */

        public void Draw()
        {
            GL.BindVertexArray(_vao);
            GL.DrawElements(
                PrimitiveType.Triangles,
                _indexCount,
                DrawElementsType.UnsignedInt,
                0
            );
            GL.BindVertexArray(0);
        }

        /* ── Sphere generation ───────────────────────────────────────────── */

        // Standard UV sphere — latitude / longitude rings.
        // Each vertex: position(3) + color(3) + texCoord(2) + normal(3) = 11 floats.
        private static void BuildSphere(Vec3 color, List<float> verts, List<uint> indices)
        {
            for (int stack = 0; stack <= Stacks; stack++)
            {
                float phi    = MathF.PI * stack / Stacks;         // 0 .. PI
                float cosPhi = MathF.Cos(phi);
                float sinPhi = MathF.Sin(phi);

                for (int slice = 0; slice <= Slices; slice++)
                {
                    float theta    = 2f * MathF.PI * slice / Slices;   // 0 .. 2PI
                    float cosTheta = MathF.Cos(theta);
                    float sinTheta = MathF.Sin(theta);

                    float x = sinPhi * cosTheta;
                    float y = cosPhi;
                    float z = sinPhi * sinTheta;

                    /* ── position ─────────────────────────────────────────── */
                    verts.Add(x * Radius);
                    verts.Add(y * Radius);
                    verts.Add(z * Radius);

                    /* ── color ────────────────────────────────────────────── */
                    verts.Add(color.X);
                    verts.Add(color.Y);
                    verts.Add(color.Z);

                    /* ── texCoord (unused) ────────────────────────────────── */
                    verts.Add((float)slice / Slices);
                    verts.Add((float)stack / Stacks);

                    /* ── normal (outward = position normalized) ───────────── */
                    verts.Add(x);
                    verts.Add(y);
                    verts.Add(z);
                }
            }

            /* ── Indices ─────────────────────────────────────────────────── */

            for (int stack = 0; stack < Stacks; stack++)
            {
                for (int slice = 0; slice < Slices; slice++)
                {
                    uint a = (uint)( stack      * (Slices + 1) + slice);
                    uint b = (uint)((stack + 1) * (Slices + 1) + slice);
                    uint c = (uint)( stack      * (Slices + 1) + slice + 1);
                    uint d = (uint)((stack + 1) * (Slices + 1) + slice + 1);

                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);

                    indices.Add(b);
                    indices.Add(d);
                    indices.Add(c);
                }
            }
        }

        /* ── Disposal ────────────────────────────────────────────────────── */

        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteVertexArray(_vao);
                GL.DeleteBuffer(_vbo);
                GL.DeleteBuffer(_ebo);
                _disposed = true;
            }
        }
    }
}