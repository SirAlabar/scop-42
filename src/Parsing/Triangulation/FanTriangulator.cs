using System.Collections.Generic;
using Scop.Math;
using Scop.Parsing.Interfaces;

namespace Scop.Parsing.Triangulation
{
    // Strategy — mandatory triangulation.
    // Connects every polygon vertex back to vertex 0 (fan method).
    // Works correctly on convex faces only.
    //
    // For a polygon (A, B, C, D, E):
    //   triangle 0 → (A, B, C)
    //   triangle 1 → (A, C, D)
    //   triangle 2 → (A, D, E)

    public class FanTriangulator : ITriangulator
    {
        /* ── ITriangulator ───────────────────────────────────────────────── */

        public List<(int A, int B, int C)> Triangulate(List<Vec3> polygon)
        {
            List<(int, int, int)> triangles = new List<(int, int, int)>();

            for (int i = 1; i + 1 < polygon.Count; i++)
            {
                triangles.Add((0, i, i + 1));
            }

            return triangles;
        }
    }
}