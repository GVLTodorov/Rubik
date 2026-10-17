using Rubik.Console;
using Rubik.Core;
using Rubik.Core.Notation;
using Rubik.Core.Rendering;

// Usage:
//   dotnet run --project backend/Rubik.Console                          -> the brief's test sequence
//   dotnet run --project backend/Rubik.Console     -- "R U R' U'"       -> a custom sequence
//   add --plain to print letters only (no ANSI colours)
const string TestSequence = "F R' U B' L D'";
const string PlainFlag = "--plain";

var plain = args.Contains(PlainFlag);
var sequence = args.Where(arg => arg != PlainFlag).DefaultIfEmpty(TestSequence).First();

try
{
    var moves = new MoveParser().Parse(sequence);
    var cube = Cube.Solved().Apply(moves);

    Console.WriteLine($"Solved cube (Green front, Red right, White up) after: {Move.Format(moves)}");
    Console.WriteLine();
    Console.WriteLine(new NetRenderer().Render(cube, AnsiStickers.Choose(plain)));
    return 0;
}
catch (MoveParseException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
