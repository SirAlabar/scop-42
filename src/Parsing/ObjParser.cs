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

        /* ── Internal types ──────────────────────────────────────────────── */

        // One vertex reference inside an OBJ face line.
        private struct FaceIdx
        {
            public int V;
            public int Vt;
            public int Vn;
        }

        // One complete OBJ face — all vertex refs + palette index.
        private struct RawFace
        {
            public FaceIdx[] Verts;
            public int       FaceIdx;
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

            List<Vec3>    positions = new List<Vec3>();
            List<Vec2>    uvs       = new List<Vec2>();
            List<Vec3>    normals   = new List<Vec3>();
            List<RawFace> rawFaces  = new List<RawFace>();

            ReadLines(filepath, positions, uvs, normals, rawFaces);

            if (positions.Count == 0)
            {
                Console.Error.WriteLine($"ObjParser: no vertices found in '{filepath}'");
                return mesh;
            }

            Vec3  centroid         = ComputeCentroid(positions);
            (Vec3 minB, Vec3 maxB) = ComputeBounds(positions, centroid);
            float scale            = ComputeScale(minB, maxB);

            Vec3 scaledMinB = minB / scale;
            Vec3 scaledMaxB = maxB / scale;

            /* ── Triangulate using injected strategy ──────────────────────── */

            List<(FaceIdx a, FaceIdx b, FaceIdx c, int faceIdx)> triangles =
                TriangulateFaces(rawFaces, positions, centroid, scale, triangulator);

            /* ── Smooth normals pre-pass ──────────────────────────────────── */

            Dictionary<int, Vec3> smoothNormals =
                ComputeSmoothNormals(triangles, positions, centroid, scale);

            /* ── Bake ─────────────────────────────────────────────────────── */

            BakeVertices(
                mesh, triangles, rawFaces, positions, uvs,
                centroid, scale, scaledMinB, scaledMaxB,
                smoothNormals, uvMapper
            );

            Console.WriteLine(
                $"ObjParser: {mesh.Vertices.Count} vertices, " +
                $"{mesh.Indices.Count / 3} triangles from '{filepath}' " +
                $"(scale = {scale:F4})"
            );

            return mesh;
        }

        /* ── Pass 1: read lines ──────────────────────────────────────────── */

        private static void ReadLines(
            string        filepath,
            List<Vec3>    positions,
            List<Vec2>    uvs,
            List<Vec3>    normals,
            List<RawFace> rawFaces)
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
                    ParseFace(tokens, rawFaces, ref faceCounter);
                }
            }
        }

        /* ── Pass 2: triangulate using injected ITriangulator ────────────── */

        // Calls triangulator.Triangulate() per face with the face's 3D positions.
        // Returns the same triangle list format used by BakeVertices.
        private static List<(FaceIdx a, FaceIdx b, FaceIdx c, int faceIdx)> TriangulateFaces(
            List<RawFace> rawFaces,
            List<Vec3>    positions,
            Vec3          centroid,
            float         scale,
            ITriangulator triangulator)
        {
            int posCount = positions.Count;

            List<(FaceIdx, FaceIdx, FaceIdx, int)> result =
                new List<(FaceIdx, FaceIdx, FaceIdx, int)>();

            foreach (RawFace face in rawFaces)
            {
                /* ── Build 3D position list for this face ─────────────────── */

                List<Vec3> facePoly = new List<Vec3>(face.Verts.Length);

                foreach (FaceIdx f in face.Verts)
                {
                    int vi = Resolve(f.V, posCount);

                    if (vi >= 1 && vi <= posCount)
                    {
                        facePoly.Add((positions[vi - 1] - centroid) / scale);
                    }
                    else
                    {
                        facePoly.Add(Vec3.Zero);
                    }
                }

                /* ── Call triangulator strategy ───────────────────────────── */

                List<(int A, int B, int C)> tris = triangulator.Triangulate(facePoly);

                foreach (var (A, B, C) in tris)
                {
                    if (A < face.Verts.Length &&
                        B < face.Verts.Length &&
                        C < face.Verts.Length)
                    {
                        result.Add((face.Verts[A], face.Verts[B], face.Verts[C], face.FaceIdx));
                    }
                }
            }

            return result;
        }

        /* ── Pass 3: centroid, bounds, scale ─────────────────────────────── */

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

        private static float ComputeScale(Vec3 minB, Vec3 maxB)
        {
            float extentX   = maxB.X - minB.X;
            float extentY   = maxB.Y - minB.Y;
            float extentZ   = maxB.Z - minB.Z;
            float maxExtent = MathF.Max(extentX, MathF.Max(extentY, extentZ));
            return MathF.Max(maxExtent / 2f, 1e-6f);
        }

        /* ── Pass 4: smooth normals ───────────────────────────────────────── */

        private static Dictionary<int, Vec3> ComputeSmoothNormals(
            List<(FaceIdx a, FaceIdx b, FaceIdx c, int faceIdx)> triangles,
            List<Vec3>                                            positions,
            Vec3                                                  centroid,
            float                                                 scale)
        {
            int posCount = positions.Count;

            Dictionary<int, Vec3> accumulated = new Dictionary<int, Vec3>();

            foreach (var (a, b, c, _) in triangles)
            {
                int viA = Resolve(a.V, posCount);
                int viB = Resolve(b.V, posCount);
                int viC = Resolve(c.V, posCount);

                if (viA < 1 || viA > posCount) { continue; }
                if (viB < 1 || viB > posCount) { continue; }
                if (viC < 1 || viC > posCount) { continue; }

                Vec3 pA = (positions[viA - 1] - centroid) / scale;
                Vec3 pB = (positions[viB - 1] - centroid) / scale;
                Vec3 pC = (positions[viC - 1] - centroid) / scale;

                Vec3 faceNormal = Vec3.Cross(pB - pA, pC - pA).Normalized();

                if (float.IsNaN(faceNormal.X))
                {
                    continue;
                }

                AccumulateNormal(accumulated, viA, faceNormal);
                AccumulateNormal(accumulated, viB, faceNormal);
                AccumulateNormal(accumulated, viC, faceNormal);
            }

            Dictionary<int, Vec3> result = new Dictionary<int, Vec3>();

            foreach (KeyValuePair<int, Vec3> kvp in accumulated)
            {
                result[kvp.Key] = kvp.Value.Normalized();
            }

            return result;
        }

        private static void AccumulateNormal(Dictionary<int, Vec3> map, int vi, Vec3 normal)
        {
            if (map.TryGetValue(vi, out Vec3 existing))
            {
                map[vi] = existing + normal;
            }
            else
            {
                map[vi] = normal;
            }
        }

        /* ── Pass 5: bake into Vertex / index arrays ─────────────────────── */

        private static void BakeVertices(
            ObjMesh                                               mesh,
            List<(FaceIdx a, FaceIdx b, FaceIdx c, int faceIdx)> triangles,
            List<RawFace>                                         rawFaces,
            List<Vec3>                                            positions,
            List<Vec2>                                            uvs,
            Vec3                                                  centroid,
            float                                                 scale,
            Vec3                                                  minB,
            Vec3                                                  maxB,
            Dictionary<int, Vec3>                                 smoothNormals,
            IUvMapper                                             uvMapper)
        {
            int posCount = positions.Count;
            int uvCount  = uvs.Count;

            foreach (var (a, b, c, faceIdx) in triangles)
            {
                Vec3 color = GrayPalette[System.Math.Abs(faceIdx) % GrayPalette.Length];

                /* ── Compute face normal for UV mapper ────────────────────── */

                int  viA = Resolve(a.V, posCount);
                int  viB = Resolve(b.V, posCount);
                int  viC = Resolve(c.V, posCount);

                Vec3 faceNormal = Vec3.UnitZ;

                if (viA >= 1 && viA <= posCount &&
                    viB >= 1 && viB <= posCount &&
                    viC >= 1 && viC <= posCount)
                {
                    Vec3 pA = (positions[viA - 1] - centroid) / scale;
                    Vec3 pB = (positions[viB - 1] - centroid) / scale;
                    Vec3 pC = (positions[viC - 1] - centroid) / scale;
                    Vec3 n  = Vec3.Cross(pB - pA, pC - pA).Normalized();

                    if (!float.IsNaN(n.X))
                    {
                        faceNormal = n;
                    }
                }

                FaceIdx[] fi = { a, b, c };

                foreach (FaceIdx f in fi)
                {
                    int vi = Resolve(f.V, posCount);

                    if (vi < 1 || vi > posCount)
                    {
                        Console.Error.WriteLine($"ObjParser: vertex index {vi} out of range");
                        continue;
                    }

                    Vec3 pos    = (positions[vi - 1] - centroid) / scale;
                    Vec2 uv     = ResolveUV(f, uvs, uvCount, pos, faceNormal, minB, maxB, uvMapper);
                    Vec3 normal = smoothNormals.TryGetValue(vi, out Vec3 n) ? n : Vec3.UnitY;

                    uint idx = (uint)mesh.Vertices.Count;
                    mesh.Vertices.Add(new Vertex(pos, color, uv, normal));
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
            string[]      tokens,
            List<RawFace> rawFaces,
            ref int       faceCounter)
        {
            FaceIdx[] verts = new FaceIdx[tokens.Length - 1];

            for (int i = 1; i < tokens.Length; i++)
            {
                verts[i - 1] = ParseFaceToken(tokens[i]);
            }

            rawFaces.Add(new RawFace { Verts = verts, FaceIdx = faceCounter });
            faceCounter++;
        }

        /* ── Face token helper ───────────────────────────────────────────── */

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

        private static int Resolve(int raw, int count)
        {
            return raw < 0 ? count + raw + 1 : raw;
        }
    }
}