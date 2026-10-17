using NUnit.Framework;
using Rubik.Api.Contracts;
using Rubik.Core;

namespace Rubik.Test.Api;

[TestFixture]
internal sealed class CubeStateMapperTests
{
    [Test]
    public void ToDto_ListsTheSixFacesInNetOrderAsColourCodes()
    {
        var dto = CubeStateMapper.ToDto(Cube.Solved());

        Assert.That(dto.Faces.Keys, Is.EqualTo(new[] { "U", "L", "F", "R", "B", "D" }));
        Assert.That(dto.Faces["F"], Is.EqualTo(Enumerable.Repeat("G", 9)));
    }
}
