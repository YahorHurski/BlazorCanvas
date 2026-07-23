using BlazorCanvas.Data.V11;

namespace BlazorCanvas.Tests.Migration;

public class V11DeterministicIdTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(3561)]
    [InlineData(3860)]
    [InlineData(3867)]
    [InlineData(int.MaxValue)]
    public void Derivation_IsDeterministic(int legacyId)
    {
        Assert.Equal(V11DeterministicId.ForFigure(legacyId), V11DeterministicId.ForFigure(legacyId));
        Assert.Equal(V11DeterministicId.ForCanvas(legacyId), V11DeterministicId.ForCanvas(legacyId));
    }

    [Fact]
    public void Derivation_HasPinnedStableTextualForms()
    {
        // These literals are regression pins: changing either re-keys an already-migrated database.
        Assert.Equal("46494755-5245-8000-8000-000000000f14", V11DeterministicId.ForFigure(3860).ToString());
        Assert.Equal("43414e56-4153-8000-8000-000000000de9", V11DeterministicId.ForCanvas(3561).ToString());
    }

    [Fact]
    public void Derivation_HasNoCollisionsAcrossNamespaces()
    {
        var figures = Enumerable.Range(1, 10_000).Select(V11DeterministicId.ForFigure).ToHashSet();
        var canvases = Enumerable.Range(1, 10_000).Select(V11DeterministicId.ForCanvas).ToHashSet();

        Assert.Equal(10_000, figures.Count);
        Assert.Equal(10_000, canvases.Count);
        Assert.Equal(20_000, figures.Concat(canvases).ToHashSet().Count);
        Assert.NotEqual(V11DeterministicId.ForFigure(42), V11DeterministicId.ForCanvas(42));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    public void Derivation_UsesVersionEightAndRfcVariant(int legacyId)
    {
        foreach (var id in new[] { V11DeterministicId.ForFigure(legacyId), V11DeterministicId.ForCanvas(legacyId) })
        {
            var bytes = id.ToByteArray(bigEndian: true);
            Assert.Equal(0x80, bytes[6] & 0xF0);
            Assert.Equal(0x80, bytes[8] & 0xC0);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Derivation_RejectsNegativeLegacyIds(int legacyId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => V11DeterministicId.ForFigure(legacyId));
        Assert.Throws<ArgumentOutOfRangeException>(() => V11DeterministicId.ForCanvas(legacyId));
    }
}
