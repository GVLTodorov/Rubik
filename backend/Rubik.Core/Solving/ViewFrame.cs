using Rubik.Core.Notation;

namespace Rubik.Core.Solving;

/// <summary>
/// Translates textbook algorithms onto this cube's fixed faces. The centres never move (there are
/// no whole-cube rotations), so instead of turning the cube, an algorithm written for "last layer
/// on top, <c>front</c> facing you" has its face letters renamed: the view's U is the real D (the
/// last layer), the view's D is the real U, and R/L/B follow from the chosen front. Turn
/// directions are unchanged because the renaming is a real rotation of the cube.
/// </summary>
internal static class ViewFrame
{
    /// <summary>The four side faces, in order around the cube.</summary>
    public static IReadOnlyList<Face> Sides { get; } = [Face.F, Face.R, Face.B, Face.L];

    private static readonly MoveParser Parser = new();

    /// <summary>The side face to the right of <paramref name="side"/>, looking at it with U on top.</summary>
    public static Face RightOf(Face side) => side switch
    {
        Face.F => Face.R,
        Face.R => Face.B,
        Face.B => Face.L,
        Face.L => Face.F,
        _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Not a side face."),
    };

    /// <summary>The side face to the left of <paramref name="side"/>, looking at it with U on top.</summary>
    public static Face LeftOf(Face side) => RightOf(RightOf(RightOf(side)));

    public static Face Opposite(Face face) => face switch
    {
        Face.U => Face.D,
        Face.D => Face.U,
        Face.L => Face.R,
        Face.R => Face.L,
        Face.F => Face.B,
        Face.B => Face.F,
        _ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
    };

    /// <summary>The real moves for <paramref name="algorithm"/> held with the last layer on top and <paramref name="front"/> in front.</summary>
    public static Move[] Translate(string algorithm, Face front)
    {
        var moves = Parser.Parse(algorithm);
        var translated = new Move[moves.Count];
        for (var i = 0; i < moves.Count; i++)
        {
            translated[i] = new Move(Map(moves[i].Face, front), moves[i].Turn);
        }

        return translated;
    }

    // Holding the cube upside down with `front` facing you, the real face on your right is the one
    // that was on the front's left.
    private static Face Map(Face viewFace, Face front) => viewFace switch
    {
        Face.U => Face.D,
        Face.D => Face.U,
        Face.F => front,
        Face.B => Opposite(front),
        Face.R => LeftOf(front),
        Face.L => RightOf(front),
        _ => throw new ArgumentOutOfRangeException(nameof(viewFace), viewFace, null),
    };
}
