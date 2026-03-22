using System;
using OpenTK.Graphics.OpenGL4;
using Scop.Parsing;

namespace Scop.Rendering
{
    public class Mesh : IDisposable
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        private int     _vao;
        private int     _vbo;
        private int     _ebo;
        private int     _indexCount;
        private bool    _disposed;

        /* ── Upload ──────────────────────────────────────────────────────── */

        // Packs ObjMesh into interleaved float array and uploads to GPU.
        //
        // Vertex layout (stride = 32 bytes):
        //   offset  0  →  location 0  →  Position  (3 floats = 12 bytes)
        //   offset 12  →  location 1  →  Color     (3 floats = 12 bytes)
        //   offset 24  →  location 2  →  TexCoord  (2 floats =  8 bytes)

        public void Upload(ObjMesh data)
        {
            _indexCount = data.Indices.Count;

            float[] vertexData = PackVertices(data);
            uint[]  indexData  = data.Indices.ToArray();

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

            /* ── Vertex attribute layout ─────────────────────────────────── */

            int stride = Vertex.Stride;

            // location 0 — position
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

            // location 1 — color
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 12);

            // location 2 — texCoord
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 24);

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

        /* ── Private helpers ─────────────────────────────────────────────── */

        // Converts List<Vertex> into a flat float[] for GL.BufferData
        private static float[] PackVertices(ObjMesh data)
        {
            float[] packed = new float[data.Vertices.Count * 8];

            for (int i = 0; i < data.Vertices.Count; i++)
            {
                Vertex v     = data.Vertices[i];
                int    start = i * 8;

                packed[start + 0] = v.Position.X;
                packed[start + 1] = v.Position.Y;
                packed[start + 2] = v.Position.Z;
                packed[start + 3] = v.Color.X;
                packed[start + 4] = v.Color.Y;
                packed[start + 5] = v.Color.Z;
                packed[start + 6] = v.TexCoord.X;
                packed[start + 7] = v.TexCoord.Y;
            }

            return packed;
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