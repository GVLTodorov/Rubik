using Rubik.Core;

namespace Rubik.Api.Contracts;

/// <summary>Converts between the API's colour-code contract and the <see cref="Cube"/> model.</summary>
public static class CubeStateMapper
{
    public static CubeStateDto ToDto(Cube cube)
    {
        var faces = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var face in Core.Faces.All)
        {
            var codes = new List<string>();
            foreach (var colour in cube.GetFace(face))
            {
                codes.Add(colour.ToCode().ToString());
            }

            faces[face.ToString()] = codes;
        }

        return new CubeStateDto { Faces = faces };
    }

    /// <summary>Each move in standard notation, e.g. <c>["F", "R'", "U2"]</c>.</summary>
    public static List<string> ToNotation(IEnumerable<Move> moves)
    {
        var notation = new List<string>();
        foreach (var move in moves)
        {
            notation.Add(move.ToString());
        }

        return notation;
    }
}
