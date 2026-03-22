using System.Collections.Generic;
using Scop.Math;

namespace Scop.Parsing.Interfaces
{
    // Strategy: contract for all triangulation algorithms.
    // ObjParser depends on this interface — never on a concrete implementation.
    //
    // Input : polygon as an ordered list of 3D positions
    // Output: list of triangles, each defined by 3 indices into the polygon list

    public interface ITriangulator
    {
        List<(int A, int B, int C)> Triangulate(List<Vec3> polygon);
    }
}