using Rubik.Core;

namespace Rubik.Test.Core;

/// <summary>
/// An independent reference model used only to check <see cref="Cube"/>'s turn table (FaceTurn): every
/// sticker is placed in 3D space and a face turn is a real 90° rotation of the stickers in that
/// layer. Axes: x points to R, y to U, z to F.
/// </summary>
internal static class GeometricCube
{
    private readonly record struct Vec(int X, int Y, int Z)
    {
        public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vec operator *(Vec a, int k) => new(a.X * k, a.Y * k, a.Z * k);

        public int Dot(Vec b) => (X * b.X) + (Y * b.Y) + (Z * b.Z);

        public Vec Cross(Vec b) => new((Y * b.Z) - (Z * b.Y), (Z * b.X) - (X * b.Z), (X * b.Y) - (Y * b.X));
    }

    // For each face: outward normal, the direction of increasing column and of increasing row,
    // as seen from outside the face (the same layout Cube documents).
    private static readonly Dictionary<Face, (Vec Normal, Vec Right, Vec Down)> Frames = new()
    {
        [Face.U] = (new(0, 1, 0), new(1, 0, 0), new(0, 0, 1)),
        [Face.D] = (new(0, -1, 0), new(1, 0, 0), new(0, 0, -1)),
        [Face.F] = (new(0, 0, 1), new(1, 0, 0), new(0, -1, 0)),
        [Face.B] = (new(0, 0, -1), new(-1, 0, 0), new(0, -1, 0)),
        [Face.R] = (new(1, 0, 0), new(0, 0, -1), new(0, -1, 0)),
        [Face.L] = (new(-1, 0, 0), new(0, 0, 1), new(0, -1, 0)),
    };

    /// <summary>Applies a clockwise quarter turn of <paramref name="turned"/> by rotating stickers in space.</summary>
    public static Cube TurnClockwise(Cube cube, Face turned)
    {
        var axis = Frames[turned].Normal;
        var result = Faces.All.ToDictionary(face => face, face => cube.GetFace(face).ToArray());

        foreach (var face in Faces.All)
        {
            for (var index = 0; index < Cube.StickersPerFace; index++)
            {
                var (position, normal) = Locate(face, index);
                if (position.Dot(axis) != 1)
                {
                    continue;
                }

                var (newFace, newIndex) = Find(RotateClockwise(position, axis), RotateClockwise(normal, axis));
                result[newFace][newIndex] = cube[face, index];
            }
        }

        return Cube.FromFaces(result.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<Colour>)pair.Value));
    }

    // Clockwise when looking at the face = -90° about its outward normal: v' = n(n·v) - n×v.
    private static Vec RotateClockwise(Vec v, Vec n) => (n * n.Dot(v)) + (n.Cross(v) * -1);

    private static (Vec Position, Vec Normal) Locate(Face face, int index)
    {
        var (normal, right, down) = Frames[face];
        var row = index / 3;
        var column = index % 3;
        return (normal + (right * (column - 1)) + (down * (row - 1)), normal);
    }

    private static (Face Face, int Index) Find(Vec position, Vec normal)
    {
        var (face, frame) = Frames.Single(pair => pair.Value.Normal == normal);
        var row = position.Dot(frame.Down) + 1;
        var column = position.Dot(frame.Right) + 1;
        return (face, (row * 3) + column);
    }
}
