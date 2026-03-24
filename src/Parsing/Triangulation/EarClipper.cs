using System;
using System.Collections.Generic;
using Scop.Math;
using Scop.Parsing.Interfaces;

namespace Scop.Parsing.Triangulation
{
    // Strategy — bonus triangulation.
    // Handles concave polygons and non-planar faces that FanTriangulator
    // gets wrong.
    //
    // Algorithm:
    //   1. Project polygon onto 2D plane using dominant axis of face normal.
    //   2. Maintain a linked list of remaining vertex indices.
    //   3. Each iteration: find an "ear" — a convex vertex whose triangle
    //      contains no other polygon vertex — clip it, repeat until 3 remain.
    //
    // Degenerate cases handled:
    //   - Zero-area triangles skipped (cross product below epsilon).
    //   - Fallback to fan triangulation if no ear found after a full pass
    //     (prevents infinite loop on degenerate input).

    public class EarClipper : ITriangulator
    {
        /* ── Constants ───────────────────────────────────────────────────── */

        private const float Epsilon = 1e-8f;

        /* ── ITriangulator ───────────────────────────────────────────────── */

        public List<(int A, int B, int C)> Triangulate(List<Vec3> polygon)
        {
            List<(int, int, int)> result = new List<(int, int, int)>();

            if (polygon.Count < 3)
            {
                return result;
            }

            if (polygon.Count == 3)
            {
                result.Add((0, 1, 2));
                return result;
            }

            /* ── Project to 2D ───────────────────────────────────────────── */

            Vec3        normal   = ComputeFaceNormal(polygon);
            List<Vec2>  poly2D   = ProjectTo2D(polygon, normal);

            /* ── Build index list ────────────────────────────────────────── */

            List<int> indices = new List<int>();

            for (int i = 0; i < polygon.Count; i++)
            {
                indices.Add(i);
            }

            /* ── Ensure counter-clockwise winding ────────────────────────── */

            if (SignedArea(poly2D, indices) < 0f)
            {
                indices.Reverse();
            }

            /* ── Ear-clipping main loop ───────────────────────────────────── */

            int safety = indices.Count * indices.Count + 10;

            while (indices.Count > 3 && safety-- > 0)
            {
                bool clipped = false;

                for (int i = 0; i < indices.Count; i++)
                {
                    int iPrev = (i + indices.Count - 1) % indices.Count;
                    int iNext = (i + 1) % indices.Count;

                    int vA = indices[iPrev];
                    int vB = indices[i];
                    int vC = indices[iNext];

                    if (!IsConvex(poly2D[vA], poly2D[vB], poly2D[vC]))
                    {
                        continue;
                    }

                    if (AnyPointInTriangle(poly2D, indices, vA, vB, vC, i))
                    {
                        continue;
                    }

                    result.Add((vA, vB, vC));
                    indices.RemoveAt(i);
                    clipped = true;
                    break;
                }

                /* ── No ear found — fall back to fan for remaining verts ─── */

                if (!clipped)
                {
                    break;
                }
            }

            /* ── Last triangle or fallback remainder ─────────────────────── */

            if (indices.Count == 3)
            {
                result.Add((indices[0], indices[1], indices[2]));
            }
            else if (indices.Count > 3)
            {
                for (int i = 1; i + 1 < indices.Count; i++)
                {
                    result.Add((indices[0], indices[i], indices[i + 1]));
                }
            }

            return result;
        }

        /* ── Face normal ─────────────────────────────────────────────────── */

        private static Vec3 ComputeFaceNormal(List<Vec3> polygon)
        {
            /* ── Newell's method — robust for non-planar polygons ─────────── */

            Vec3 normal = Vec3.Zero;

            for (int i = 0; i < polygon.Count; i++)
            {
                Vec3 cur  = polygon[i];
                Vec3 next = polygon[(i + 1) % polygon.Count];

                normal.X += (cur.Y - next.Y) * (cur.Z + next.Z);
                normal.Y += (cur.Z - next.Z) * (cur.X + next.X);
                normal.Z += (cur.X - next.X) * (cur.Y + next.Y);
            }

            Vec3 n = normal.Normalized();

            if (float.IsNaN(n.X))
            {
                return Vec3.UnitZ;
            }

            return n;
        }

        /* ── 2D projection ───────────────────────────────────────────────── */

        // Drop the dominant-axis component and keep the other two.
        private static List<Vec2> ProjectTo2D(List<Vec3> polygon, Vec3 normal)
        {
            float absX = MathF.Abs(normal.X);
            float absY = MathF.Abs(normal.Y);
            float absZ = MathF.Abs(normal.Z);

            List<Vec2> result = new List<Vec2>(polygon.Count);

            foreach (Vec3 v in polygon)
            {
                if (absX >= absY && absX >= absZ)
                {
                    result.Add(new Vec2(v.Y, v.Z));
                }
                else if (absY >= absX && absY >= absZ)
                {
                    result.Add(new Vec2(v.X, v.Z));
                }
                else
                {
                    result.Add(new Vec2(v.X, v.Y));
                }
            }

            return result;
        }

        /* ── Signed area (shoelace) ──────────────────────────────────────── */

        private static float SignedArea(List<Vec2> poly2D, List<int> indices)
        {
            float area = 0f;
            int   n    = indices.Count;

            for (int i = 0; i < n; i++)
            {
                Vec2 cur  = poly2D[indices[i]];
                Vec2 next = poly2D[indices[(i + 1) % n]];
                area     += cur.X * next.Y - next.X * cur.Y;
            }

            return area * 0.5f;
        }

        /* ── Convexity test (cross product Z sign) ───────────────────────── */

        private static bool IsConvex(Vec2 a, Vec2 b, Vec2 c)
        {
            float cross = (b.X - a.X) * (c.Y - a.Y)
                        - (b.Y - a.Y) * (c.X - a.X);
            return cross > Epsilon;
        }

        /* ── Point-in-triangle (barycentric) ─────────────────────────────── */

        private static bool PointInTriangle(Vec2 p, Vec2 a, Vec2 b, Vec2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);

            bool hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
            bool hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);

            return !(hasNeg && hasPos);
        }

        private static float Sign(Vec2 p, Vec2 a, Vec2 b)
        {
            return (p.X - b.X) * (a.Y - b.Y)
                 - (a.X - b.X) * (p.Y - b.Y);
        }

        // Returns true if any vertex in the remaining polygon (excluding vA, vB, vC)
        // falls strictly inside the candidate ear triangle.
        private static bool AnyPointInTriangle(
            List<Vec2> poly2D,
            List<int>  indices,
            int        vA,
            int        vB,
            int        vC,
            int        skipIndex)
        {
            Vec2 a = poly2D[vA];
            Vec2 b = poly2D[vB];
            Vec2 c = poly2D[vC];

            for (int i = 0; i < indices.Count; i++)
            {
                if (i == skipIndex)
                {
                    continue;
                }

                int  vi = indices[i];

                if (vi == vA || vi == vB || vi == vC)
                {
                    continue;
                }

                if (PointInTriangle(poly2D[vi], a, b, c))
                {
                    return true;
                }
            }

            return false;
        }
    }
}