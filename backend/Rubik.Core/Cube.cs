using System.Diagnostics;
using System.Text;
using Rubik.Core.Rendering;

namespace Rubik.Core;

/// <summary>
/// An immutable 3×3 cube. Each face holds 9 stickers indexed 0–8, row-major, as seen when looking
/// straight at that face from outside:
/// <code>
/// 0 1 2
/// 3 4 5
/// 6 7 8
/// </code>
/// Row 0 of U is next to B, row 0 of D is next to F, and row 0 of F, R, B and L is next to U.
/// Index 4 is the centre. The orientation is fixed: Green front, Red right, White up when solved.
/// How a turn moves the stickers is in <see cref="FaceTurn"/>.
/// </summary>
[DebuggerDisplay("{IsSolved ? \"Solved\" : \"Scrambled\",nq}")]
[DebuggerTypeProxy(typeof(DebugView))]
public sealed class Cube : IEquatable<Cube>
{
    /// <summary>Number of stickers on one face.</summary>
    public const int StickersPerFace = 9;

    /// <summary>Index of the centre sticker on every face.</summary>
    public const int CentreIndex = 4;

    /// <summary>One 9-sticker array per face, indexed by <see cref="Face"/>: <c>_faces[(int)Face.F][4]</c>.</summary>
    private readonly Colour[][] _faces;

    private Cube(Colour[][] faces)
    {
        _faces = faces;
    }

    /// <summary>A solved cube: Green front, Red right, White up.</summary>
    public static Cube Solved()
    {
        var faces = new Colour[Faces.Count][];
        foreach (var face in Faces.All)
        {
            var stickers = new Colour[StickersPerFace];
            for (var i = 0; i < StickersPerFace; i++)
            {
                stickers[i] = face.SolvedColour();
            }

            faces[(int)face] = stickers;
        }

        return new Cube(faces);
    }

    /// <summary>
    /// Builds a cube from explicit face colours. The state must have all six faces, nine stickers per
    /// face, and nine stickers of each colour. Solvability (parity, twist) is not checked.
    /// </summary>
    /// <exception cref="InvalidCubeStateException">The state is malformed.</exception>
    public static Cube FromFaces(IReadOnlyDictionary<Face, IReadOnlyList<Colour>> faces)
    {
        ArgumentNullException.ThrowIfNull(faces);

        var result = new Colour[Faces.Count][];
        var colourCounts = new int[ColourCodes.All.Count];
        foreach (var face in Faces.All)
        {
            if (!faces.TryGetValue(face, out var colours) || colours is null)
            {
                throw new InvalidCubeStateException($"Face '{face}' is missing.");
            }

            if (colours.Count != StickersPerFace)
            {
                throw new InvalidCubeStateException(
                    $"Face '{face}' has {colours.Count} stickers; expected {StickersPerFace}.");
            }

            var stickers = new Colour[StickersPerFace];
            for (var i = 0; i < StickersPerFace; i++)
            {
                stickers[i] = colours[i];
                colourCounts[(int)colours[i]]++;
            }

            result[(int)face] = stickers;
        }

        foreach (var colour in ColourCodes.All)
        {
            var count = colourCounts[(int)colour];
            if (count != StickersPerFace)
            {
                throw new InvalidCubeStateException(
                    $"Colour '{colour.ToCode()}' appears {count} times; every colour must appear exactly {StickersPerFace} times.");
            }
        }

        return new Cube(result);
    }

    /// <summary>The colour of one sticker.</summary>
    public Colour this[Face face, int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, StickersPerFace);
            return _faces[(int)face][index];
        }
    }

    /// <summary>The nine stickers of a face, in index order.</summary>
    public IReadOnlyList<Colour> GetFace(Face face) => (Colour[])_faces[(int)face].Clone();

    /// <summary>True when every face is a single colour.</summary>
    public bool IsSolved
    {
        get
        {
            foreach (var stickers in _faces)
            {
                for (var i = 0; i < StickersPerFace; i++)
                {
                    if (stickers[i] != stickers[CentreIndex])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Returns the cube after applying one move. This instance is not changed. Every move is one or
    /// more clockwise quarter turns: <c>F</c> = 1, <c>F2</c> = 2, <c>F'</c> = 3.
    /// </summary>
    public Cube Apply(Move move)
    {
        // Copy every face, so the turn below never touches this cube.
        var faces = new Colour[Faces.Count][];
        for (var f = 0; f < Faces.Count; f++)
        {
            faces[f] = (Colour[])_faces[f].Clone();
        }

        for (var turn = 0; turn < move.QuarterTurns; turn++)
        {
            FaceTurn.Clockwise(faces, move.Face);
        }

        return new Cube(faces);
    }

    /// <summary>Returns the cube after applying the moves in order. This instance is not changed.</summary>
    public Cube Apply(IEnumerable<Move> moves)
    {
        ArgumentNullException.ThrowIfNull(moves);

        var cube = this;
        foreach (var move in moves)
        {
            cube = cube.Apply(move);
        }

        return cube;
    }

    /// <inheritdoc />
    public bool Equals(Cube? other)
    {
        if (other is null)
        {
            return false;
        }

        for (var f = 0; f < Faces.Count; f++)
        {
            for (var i = 0; i < StickersPerFace; i++)
            {
                if (_faces[f][i] != other._faces[f][i])
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Cube other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var stickers in _faces)
        {
            foreach (var sticker in stickers)
            {
                hash.Add(sticker);
            }
        }

        return hash.ToHashCode();
    }

    /// <summary>The cube as six space-separated rows of colour codes, e.g. <c>U: W W W W W W W W W</c>.</summary>
    public override string ToString()
    {
        var text = new StringBuilder();
        foreach (var face in Faces.All)
        {
            if (text.Length > 0)
            {
                text.Append(Environment.NewLine);
            }

            text.Append(face).Append(": ").Append(Codes(face));
        }

        return text.ToString();
    }

    /// <summary>One face as colour codes, e.g. <c>W W W W W W W W W</c>.</summary>
    private string Codes(Face face)
    {
        var codes = new char[StickersPerFace];
        for (var i = 0; i < StickersPerFace; i++)
        {
            codes[i] = _faces[(int)face][i].ToCode();
        }

        return string.Join(' ', codes);
    }

    /// <summary>What the debugger shows when a <see cref="Cube"/> is expanded: each face, then the net.</summary>
    private sealed class DebugView(Cube cube)
    {
        public string U => cube.Codes(Face.U);

        public string L => cube.Codes(Face.L);

        public string F => cube.Codes(Face.F);

        public string R => cube.Codes(Face.R);

        public string B => cube.Codes(Face.B);

        public string D => cube.Codes(Face.D);

        /// <summary>Open with the text visualizer to see the exploded view.</summary>
        public string Net => new NetRenderer().Render(cube);
    }
}
