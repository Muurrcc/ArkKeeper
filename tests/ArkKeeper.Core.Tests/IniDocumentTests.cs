using ArkKeeper.Core.Ini;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class IniDocumentTests
{
    [Fact]
    public void Parse_ReadsSectionsAndKeys()
    {
        const string text = "[ServerSettings]\nServerPVE=True\nDifficultyOffset=1.5\n\n[SessionSettings]\nSessionName=My Server\n";

        var document = IniDocument.Parse(text);

        Assert.Equal("True", document.FindSection("ServerSettings")!.GetSingle("ServerPVE"));
        Assert.Equal("1.5", document.FindSection("ServerSettings")!.GetSingle("DifficultyOffset"));
        Assert.Equal("My Server", document.FindSection("SessionSettings")!.GetSingle("SessionName"));
    }

    [Fact]
    public void Parse_IgnoresCommentsAndBlankLines()
    {
        const string text = "; a comment\n[ServerSettings]\n\n# another comment\nServerPVE=True\n";

        var document = IniDocument.Parse(text);

        Assert.Equal("True", document.FindSection("ServerSettings")!.GetSingle("ServerPVE"));
    }

    [Fact]
    public void Section_PreservesRepeatedKeysInOrder()
    {
        var document = new IniDocument();
        var section = document.GetOrAddSection("ServerSettings");
        section.Add("ConfigOverrideItemMaxQuantity", "First");
        section.Add("ConfigOverrideItemMaxQuantity", "Second");

        Assert.Equal(new[] { "First", "Second" }, section.GetAll("ConfigOverrideItemMaxQuantity"));
    }

    [Fact]
    public void RoundTrip_WriteThenParse_ProducesEquivalentDocument()
    {
        var original = new IniDocument();
        original.GetOrAddSection("ServerSettings").SetSingle("XPMultiplier", "2.5");

        var reparsed = IniDocument.Parse(original.ToString());

        Assert.Equal("2.5", reparsed.FindSection("ServerSettings")!.GetSingle("XPMultiplier"));
    }
}
