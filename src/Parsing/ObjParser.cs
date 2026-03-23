using System;
using System.Collections.Generic;
using System.IO;
using Scop.Math;
using Scop.Parsing.Interfaces;
using Scop.Parsing.Triangulation;
using Scop.Parsing.UvMapping;
using Scop.Utils;

namespace Scop.Parsing
{
    public class ObjMesh
    {
        /* ── Fields ──────────────────────────────────────────────────────── */

        public List<Vertex> Vertices { get; } = new List<Vertex>();
        public List<uint>   Indices  { get; } = new List<uint>();
    }

    public static class ObjParser
    {
        /* ── Gray palette (8 shades, one per face index mod 8) ───────────── */

        private static readonly Vec3[] GrayPalette =
        {
            new Vec3(0.15f, 0.15f, 0.15f),
            new Vec3(0.25f, 0.25f, 0.25f),
            new Vec3(0.35f, 0.35f, 0.35f),
            new Vec3(0.45f, 0.45f, 0.45f),
            new Vec3(0.55f, 0.55f, 0.55f),
            new Vec3(0.65f, 0.65f, 0.65f),
            new Vec3(0.75f, 0.75f, 0.75f),
            new Vec3(0.85f, 0.85f, 0.85f),
        };

        /* ── Internal face index (1-based, 0 = not provided) ────────────── */

        private struct FaceIdx
        {
            public int V;
            public int Vt;
            public int Vn;
        }

        /* ── Public entry point ──────────────────────────────────────────── */

        public static ObjMesh Parse(
            string         filepath,
            ITriangulator? triangulator = null,
            IUvMapper?     uvMapper     = null)
        {
            ObjMesh mesh = new ObjMesh();

            if (!FileValidator.Validate(filepath, "ObjParser"))
            {
                return mesh;
            }

            if (triangulator == null) { triangulator = new FanTriangulator(); }
            if (uvMapper == null)     { uvMapper     = new BoxUvMapper(); }

            List<Vec3> positions = new List<Vec3>();
            List<Vec2> uvs       = new List<Vec2>();
            List<Vec3> normals   = new List<Vec3>();

            List<(FaceIdx a, FaceIdx b, FaceIdx c, int faceIdx)> triangles
                = new List<(FaceIdx, FaceIdx, FaceIdx, int)>();

            ReadLines(filepath, positions, uvs, normals, triangles);

            if (positions.Count == 0)
            {
                Console.Error.WriteLine($"ObjParser: no vertices found in '{filepath}'");
                return mesh;
            }

            Vec3  centroid         = ComputeCentroid(positions);
            (Vec3 minB, Vec3 maxB) = ComputeBounds(positions, centroid);
            float scale            = ComputeScale(minB, maxB);

            /* ── Scale bounds for UV mapper ───────────────────────────────── */

            Vec3 scaledMinB = minB / scale;
            Vec3 scaledMaxB = maxB / scale;

            BakeVertices(mesh, triangles, positions, uvs, centroid, scale, scaledMinB, scaledMaxB, uvMapper);

            Console.WriteLine(
                $"ObjParser: {mesh.Vertices.Count} vertices, " +
                $"{mesh.Indices.Count / 3} triangles from '{filepath}' " +
                $"(scale = {scale:F4})"
            );

            return mesh;
        }

        /* ── Pass 1: read lines ──────────────────────────────────────────── */

        private static void ReadLines(
            string                                                filepath,
            List<Vec3>                                            positions,
            List<Vec2>                                            uvs,
            List<Vec3>                                            normals,
            List<(FaceIdx a, FaceIdx b, FaceIdx c, int faceIdx)> triangles)
        {
            int faceCounter = 0;

            foreach (string rawLine in File.ReadLines(filepath))
            {
                string[] tokens = FileParser.TokenizeLine(rawLine);

                if (tokens.Length == 0)
                {
                    continue;
                }

                string kw = tokens[0];

                if (kw == "v" && tokens.Length >= 4)
                {
                    ParsePosition(tokens, positions);
                }
                else if (kw == "vt" && tokens.Length >= 3)
                {
                    ParseTexCoord(tokens, uvs);
                }
                else if (kw == "vn" && tokens.Length >= 4)
                {
                    ParseNormal(tokens, normals);
                }
                else if (kw == "f" && tokens.Length >= 4)
                {
                    ParseFace(tokens, triangles, ref faceCounter);
                }
            }
        }

        /* ── Pass 2: centroid, bounds, scale ─────────────────────────────── */

        private static Vec3 ComputeCentroid(List<Vec3> positions)
        {
            Vec3 sum = Vec3.Zero;

            foreach (Vec3 p in positions)
            {
                sum.X += p.X;
                sum.Y += p.Y;
                sum.Z += p.Z;
            }

            return sum / positions.Count;
        }

        private static (Vec3 minB, Vec3 maxB) ComputeBounds(List<Vec3> positions, Vec3 centroid)
        {
            Vec3 minB = new Vec3( float.MaxValue,  float.MaxValue,  float.MaxValue);
            Vec3 maxB = new Vec3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

            foreach (Vec3 p in positions)
            {
                Vec3 c = p - centroid;

                if (c.X < minB.X) { minB.X = c.X; }
                if (c.Y < minB.Y) { minB.Y = c.Y; }
                if (c.Z < minB.Z) { minB.Z = c.Z; }
                if (c.X > maxB.X) { maxB.X = c.X; }
                if (c.Y > maxB.Y) { maxB.Y = c.Y; }
                if (c.Z > maxB.Z) { maxB.Z = c.Z; }
            }

            return (minB, maxB);
        }

        // Bonus 3 — compute uniform scale so largest axis fits in [-1, +1] (2 units total).
        // A floor of 1e-6 guards against degenerate zero-size meshes.
        private static float ComputeScale(Vec3 minB, Vec3 maxB)
        {
            float extentX = maxB.X - minB.X;
            float extentY = maxB.Y - minB.Y;
            float extentZ = maxB.Z - minB.Z;

            float maxExtent = MathF.Max(extentX, MathF.Max(extentY, extentZ));

            return MathF.Max(maxExtent / 2f, 1e-6f);
        }

        /* ── Pass 3: bake into Vertex / index arrays ─────────────────────── */

        private static void BakeVertices(
            ObjMesh                                               mesh,
            List<(FaceIdx a, FaceIdx b, FaceIdx c, int faceIdx)> triangles,
            List<Vec3>                                            positions,
            List<Vec2>                                            uvs,
            Vec3                                                  centroid,
            float                                                 scale,
            Vec3                                                  minB,
            Vec3                                                  maxB,
            IUvMapper                                             uvMapper)
        {
            int posCount = positions.Count;
            int uvCount  = uvs.Count;

            foreach (var (a, b, c, faceIdx) in triangles)
            {
                Vec3     color = GrayPalette[System.Math.Abs(faceIdx) % GrayPalette.Length];
                FaceIdx[] fi   = { a, b, c };

                foreach (FaceIdx f in fi)
                {
                    int vi = Resolve(f.V, posCount);

                    if (vi < 1 || vi > posCount)
                    {
                        Console.Error.WriteLine($"ObjParser: vertex index {vi} out of range");
                        continue;
                    }

                    /* ── Center then scale to 2-unit bounding box ─────────── */

                    Vec3 pos = (positions[vi - 1] - centroid) / scale;
                    Vec2 uv  = ResolveUV(f, uvs, uvCount, pos, Vec3.Zero, minB, maxB, uvMapper);

                    uint idx = (uint)mesh.Vertices.Count;
                    mesh.Vertices.Add(new Vertex(pos, color, uv));
                    mesh.Indices.Add(idx);
                }
            }
        }

        /* ── Line parsing helpers ────────────────────────────────────────── */

        private static void ParsePosition(string[] tokens, List<Vec3> positions)
        {
            float x = FileParser.ParseFloat(tokens[1]);
            float y = FileParser.ParseFloat(tokens[2]);
            float z = FileParser.ParseFloat(tokens[3]);
            positions.Add(new Vec3(x, y, z));
        }

        private static void ParseTexCoord(string[] tokens, List<Vec2> uvs)
        {
            float u = FileParser.ParseFloat(tokens[1]);
            float v = FileParser.ParseFloat(tokens[2]);
            uvs.Add(new Vec2(u, v));
        }

        private static void ParseNormal(string[] tokens, List<Vec3> normals)
        {
            float x = FileParser.ParseFloat(tokens[1]);
            float y = FileParser.ParseFloat(tokens[2]);
            float z = FileParser.ParseFloat(tokens[3]);
            normals.Add(new Vec3(x, y, z));
        }

        private static void ParseFace(
            string[]                                              tokens,
            List<(FaceIdx a, FaceIdx b, FaceIdx c, int faceIdx)> triangles,
            ref int                                               faceCounter)
        {
            FaceIdx[] verts = new FaceIdx[tokens.Length - 1];

            for (int i = 1; i < tokens.Length; i++)
            {
                verts[i - 1] = ParseFaceToken(tokens[i]);
            }

            for (int i = 1; i + 1 < verts.Length; i++)
            {
                triangles.Add((verts[0], verts[i], verts[i + 1], faceCounter));
            }

            faceCounter++;
        }

        /* ── Face token helper ───────────────────────────────────────────── */

        // Parse one face token: "v", "v/vt", "v/vt/vn", "v//vn"
        private static FaceIdx ParseFaceToken(string token)
        {
            FaceIdx  idx   = default;
            string[] parts = token.Split('/');

            if (parts.Length > 0 && parts[0].Length > 0)
            {
                idx.V = int.Parse(parts[0]);
            }

            if (parts.Length > 1 && parts[1].Length > 0)
            {
                idx.Vt = int.Parse(parts[1]);
            }

            if (parts.Length > 2 && parts[2].Length > 0)
            {
                idx.Vn = int.Parse(parts[2]);
            }

            return idx;
        }

        /* ── UV resolution helper ────────────────────────────────────────── */

        private static Vec2 ResolveUV(
            FaceIdx    f,
            List<Vec2> uvs,
            int        uvCount,
            Vec3       pos,
            Vec3       faceNormal,
            Vec3       minB,
            Vec3       maxB,
            IUvMapper  uvMapper)
        {
            if (f.Vt != 0)
            {
                int vti = Resolve(f.Vt, uvCount);

                if (vti >= 1 && vti <= uvCount)
                {
                    return uvs[vti - 1];
                }
            }

            return uvMapper.Map(pos, faceNormal, minB, maxB);
        }

        /* ── Math helpers ────────────────────────────────────────────────── */

        // Resolve a potentially negative (relative) index to 1-based positive
        private static int Resolve(int raw, int count)
        {
            return raw < 0 ? count + raw + 1 : raw;
        }
    }
}