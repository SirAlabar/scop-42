using Scop.Math;

namespace Scop.Parsing.Interfaces
{
    // Strategy: contract for all UV mapping algorithms.
    // ObjParser depends on this interface — never on a concrete implementation.
    //
    // Input : centered vertex position, face normal, bounding box
    // Output: UV coordinate in [0..1] range

    public interface IUvMapper
    {
        Vec2 Map(Vec3 position, Vec3 faceNormal, Vec3 minBound, Vec3 maxBound);
    }
}