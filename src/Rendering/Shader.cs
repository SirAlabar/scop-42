using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using Scop.Utils;
using Scop.Math;

namespace Scop.Rendering
{
    public class Shader : IDisposable
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        private int                         _id;
        private bool                        _disposed;
        private Dictionary<string, int>     _locationCache;

        /* ── Constructor ─────────────────────────────────────────────────── */

        public Shader()
        {
            _locationCache = new Dictionary<string, int>();
        }

        /* ── Public entry point ──────────────────────────────────────────── */

        public bool Load(string vertPath, string fragPath)
        {
            string? vertSrc = ReadFile(vertPath);
            string? fragSrc = ReadFile(fragPath);

            if (vertSrc == null || fragSrc == null)
            {
                return false;
            }

            int vert = CompileShader(ShaderType.VertexShader,   vertSrc);
            int frag = CompileShader(ShaderType.FragmentShader, fragSrc);

            if (vert == 0 || frag == 0)
            {
                if (vert != 0) { GL.DeleteShader(vert); }
                if (frag != 0) { GL.DeleteShader(frag); }
                return false;
            }

            _id = GL.CreateProgram();
            GL.AttachShader(_id, vert);
            GL.AttachShader(_id, frag);
            GL.LinkProgram(_id);

            // Shaders are copied into the program — safe to delete now
            GL.DeleteShader(vert);
            GL.DeleteShader(frag);

            GL.GetProgram(_id, GetProgramParameterName.LinkStatus, out int status);

            if (status == 0)
            {
                Console.Error.WriteLine($"Shader: link error:\n{GL.GetProgramInfoLog(_id)}");
                return false;
            }

            CacheUniformLocations();
            return true;
        }

        public void Use()
        {
            GL.UseProgram(_id);
        }

        /* ── Uniform setters ─────────────────────────────────────────────── */

        public void SetInt(string name, int value)
        {
            GL.Uniform1(GetLocation(name), value);
        }

        public void SetFloat(string name, float value)
        {
            GL.Uniform1(GetLocation(name), value);
        }

        public void SetVec3(string name, Vec3 v)
        {
            GL.Uniform3(GetLocation(name), v.X, v.Y, v.Z);
        }

        public void SetMat4(string name, Mat4 mat)
        {
            float[] data = mat.ToArray();
            GL.UniformMatrix4(GetLocation(name), 1, false, data);
        }

        /* ── Location cache ──────────────────────────────────────────────── */

        // Called once after linking — queries all uniform locations and stores them
        private void CacheUniformLocations()
        {
            GL.GetProgram(_id, GetProgramParameterName.ActiveUniforms, out int count);

            for (int i = 0; i < count; i++)
            {
                string name = GL.GetActiveUniform(_id, i, out _, out _);
                int    loc  = GL.GetUniformLocation(_id, name);
                _locationCache[name] = loc;
            }
        }

        // Returns cached location — no GPU call at runtime
        private int GetLocation(string name)
        {
            if (_locationCache.TryGetValue(name, out int loc))
            {
                return loc;
            }

            Console.Error.WriteLine($"Shader: uniform '{name}' not found in cache");
            return -1;
        }

        /* ── File helpers ────────────────────────────────────────────────── */

        private static string? ReadFile(string path)
        {
            if (!FileValidator.Validate(path, "Shader"))
            {
                return null;
            }

            return File.ReadAllText(path);
        }

        /* ── Compile helper ──────────────────────────────────────────────── */

        private static int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);

            if (status == 0)
            {
                string typeName = type == ShaderType.VertexShader ? "VERTEX" : "FRAGMENT";
                Console.Error.WriteLine(
                    $"Shader: compile error ({typeName}):\n{GL.GetShaderInfoLog(shader)}"
                );
                GL.DeleteShader(shader);
                return 0;
            }

            return shader;
        }

        /* ── Disposal ────────────────────────────────────────────────────── */

        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteProgram(_id);
                _locationCache.Clear();
                _disposed = true;
            }
        }
    }
}