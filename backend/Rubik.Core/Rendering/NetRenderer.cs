using System.Text;

namespace Rubik.Core.Rendering;

/// <summary>Renders a cube as an exploded (net) text view.</summary>
public interface INetRenderer
{
    /// <summary>
    /// Renders the net: U on top, then L F R B side by side, then D, separated by blank lines.
    /// </summary>
    /// <param name="cube">The cube to render.</param>
    /// <param name="formatSticker">
    /// Optional sticker formatter, e.g. to add ANSI colours. It must produce text that is one
    /// character wide on screen. Defaults to the colour code (W O G R B Y).
    /// </param>
    string Render(Cube cube, Func<Colour, string>? formatSticker = null);
}

/// <summary>
/// The exploded view required by the brief:
/// <code>
///         U U U
///         U U U
///         U U U
///
/// L L L   F F F   R R R   B B B
/// ...
///
///         D D D
/// </code>
/// </summary>
public sealed class NetRenderer : INetRenderer
{
    private const int Size = 3;
    private const char StickerSeparator = ' ';
    private const string FaceSeparator = "   ";
    private const char NewLine = '\n';

    private static readonly Face[] MiddleRow = [Face.L, Face.F, Face.R, Face.B];

    // A face row is "X X X" (5 characters); the indent lines U and D up with F.
    private static readonly string Indent = new(' ', (2 * Size) - 1 + FaceSeparator.Length);

    /// <inheritdoc />
    public string Render(Cube cube, Func<Colour, string>? formatSticker = null)
    {
        ArgumentNullException.ThrowIfNull(cube);
        formatSticker ??= colour => colour.ToCode().ToString();

        var text = new StringBuilder();

        // U on top, indented to line up with F.
        for (var row = 0; row < Size; row++)
        {
            text.Append(Indent).Append(FormatRow(cube, Face.U, row, formatSticker)).Append(NewLine);
        }

        text.Append(NewLine);

        // L F R B side by side.
        for (var row = 0; row < Size; row++)
        {
            for (var i = 0; i < MiddleRow.Length; i++)
            {
                if (i > 0)
                {
                    text.Append(FaceSeparator);
                }

                text.Append(FormatRow(cube, MiddleRow[i], row, formatSticker));
            }

            text.Append(NewLine);
        }

        text.Append(NewLine);

        // D at the bottom, indented like U. No newline after the last row.
        for (var row = 0; row < Size; row++)
        {
            if (row > 0)
            {
                text.Append(NewLine);
            }

            text.Append(Indent).Append(FormatRow(cube, Face.D, row, formatSticker));
        }

        return text.ToString();
    }

    /// <summary>One row of a face, e.g. <c>W W W</c>.</summary>
    private static string FormatRow(Cube cube, Face face, int row, Func<Colour, string> formatSticker)
    {
        var text = new StringBuilder();
        for (var column = 0; column < Size; column++)
        {
            if (column > 0)
            {
                text.Append(StickerSeparator);
            }

            text.Append(formatSticker(cube[face, (row * Size) + column]));
        }

        return text.ToString();
    }
}
