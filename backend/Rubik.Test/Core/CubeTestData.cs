using NUnit.Framework;
using Rubik.Core;

namespace Rubik.Test.Core;

/// <summary>Shared data: the brief's acceptance sequence and helpers to read faces as colour codes.</summary>
internal static class CubeTestData
{
    public const string AcceptanceSequence = "F R' U B' L D'";

    /// <summary>The expected faces after <see cref="AcceptanceSequence"/> on a solved cube, as shown in the brief.</summary>
    public static IReadOnlyDictionary<Face, string> AcceptanceFaces { get; } = new Dictionary<Face, string>
    {
        [Face.U] = "R O G B W W B B B",
        [Face.L] = "G Y Y O O G B G O",
        [Face.F] = "O R R O G W W W W",
        [Face.R] = "Y B O R R W O Y R",
        [Face.B] = "Y B W O B Y Y Y W",
        [Face.D] = "G G B R Y R R G G",
    };

    /// <summary>The expected net after <see cref="AcceptanceSequence"/>, as shown in the brief.</summary>
    public const string AcceptanceNet =
        "        R O G\n" +
        "        B W W\n" +
        "        B B B\n" +
        "\n" +
        "G Y Y   O R R   Y B O   Y B W\n" +
        "O O G   O G W   R R W   O B Y\n" +
        "B G O   W W W   O Y R   Y Y W\n" +
        "\n" +
        "        G G B\n" +
        "        R Y R\n" +
        "        R G G";

    public static IEnumerable<Face> AllFaces => Faces.All;

    public static string Codes(this Cube cube, Face face) =>
        string.Join(' ', cube.GetFace(face).Select(colour => colour.ToCode()));

    public static string Codes(this Cube cube, Face face, params int[] indices) =>
        string.Join(' ', indices.Select(index => cube[face, index].ToCode()));
}
