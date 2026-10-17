using System.Reflection;
using NUnit.Framework;

namespace Rubik.Integration.Test;

/// <summary>
/// Runs the console runner's real entry point, as <c>dotnet run --project backend/Rubik.Console</c>
/// would. It swaps the process-wide Console.Out/Error, so it must not run alongside other tests.
/// </summary>
[TestFixture]
[NonParallelizable]
internal sealed class ConsoleRunnerTests
{
    private const string AcceptanceNet =
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

    private static (int ExitCode, string Output, string Error) Run(params string[] args)
    {
        var entryPoint = Assembly.Load("Rubik.Console").EntryPoint!;
        var (originalOut, originalError) = (System.Console.Out, System.Console.Error);
        using var output = new StringWriter();
        using var error = new StringWriter();

        System.Console.SetOut(output);
        System.Console.SetError(error);
        try
        {
            var exitCode = (int)entryPoint.Invoke(null, [args])!;
            return (exitCode, output.ToString().ReplaceLineEndings("\n"), error.ToString());
        }
        finally
        {
            System.Console.SetOut(originalOut);
            System.Console.SetError(originalError);
        }
    }

    [Test]
    public void NoArguments_PrintsTheBriefsAcceptanceNet()
    {
        var (exitCode, output, error) = Run("--plain");

        Assert.That(exitCode, Is.EqualTo(0));
        Assert.That(error, Is.Empty);
        Assert.That(output, Does.Contain("after: F R' U B' L D'"));
        Assert.That(output, Does.Contain(AcceptanceNet));
    }

    [Test]
    public void CustomSequence_IsAppliedInstead()
    {
        var (exitCode, output, _) = Run("R R'", "--plain");

        Assert.That(exitCode, Is.EqualTo(0));
        Assert.That(output, Does.Contain("after: R R'"));
        Assert.That(output, Does.Contain("O O O   G G G   R R R   B B B"));
    }

    [Test]
    public void InvalidSequence_PrintsTheErrorAndExitsWithOne()
    {
        var (exitCode, output, error) = Run("F Q", "--plain");

        Assert.That(exitCode, Is.EqualTo(1));
        Assert.That(output, Is.Empty);
        Assert.That(error, Does.Contain("'Q'"));
    }
}
