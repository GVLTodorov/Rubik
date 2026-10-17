using System.ComponentModel.DataAnnotations;

namespace Rubik.Api.Contracts;

/// <summary>
/// A cube state: six faces (U, L, F, R, B, D), each with nine colour codes from W O G R B Y,
/// row-major as seen from outside the face. Every colour must appear exactly nine times.
/// </summary>
public sealed record CubeStateDto
{
    /// <summary>Face letter → nine colour codes.</summary>
    [Required]
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> Faces { get; init; }
}

/// <summary>A random move sequence.</summary>
public sealed record ScrambleResponse
{
    /// <summary>Space-separated moves that never turn the same face twice in a row.</summary>
    /// <example>R U2 F' L D B2 U' R2 F L' D2 B U R' F2 D' L2 B' U2 R</example>
    public required string Moves { get; init; }
}
