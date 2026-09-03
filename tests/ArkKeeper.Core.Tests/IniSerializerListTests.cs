using ArkKeeper.Core.Ini;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class IniSerializerListTests
{
    private sealed class ListSettings
    {
        [IniSetting(IniFile.Game, "TestSection", "Overrides")]
        public List<string> Overrides { get; set; } = new();
    }

    [Fact]
    public void Write_WithMultipleListItems_WritesOneRepeatedKeyPerItem()
    {
        var settings = new ListSettings();
        settings.Overrides.Add("(ClassName=\"Raptor_Character_BP_C\",Multiplier=2.0)");
        settings.Overrides.Add("(ClassName=\"Rex_Character_BP_C\",Multiplier=1.5)");

        var document = IniSerializer.Write(settings, IniFile.Game);
        var section = document.FindSection("TestSection")!;

        Assert.Equal(
            new[]
            {
                "(ClassName=\"Raptor_Character_BP_C\",Multiplier=2.0)",
                "(ClassName=\"Rex_Character_BP_C\",Multiplier=1.5)",
            },
            section.GetAll("Overrides"));
    }

    [Fact]
    public void Write_WithEmptyList_WritesNoEntriesForThatKey()
    {
        var settings = new ListSettings();

        var document = IniSerializer.Write(settings, IniFile.Game);

        Assert.Empty(document.FindSection("TestSection")!.GetAll("Overrides"));
    }

    [Fact]
    public void Apply_ReadsRepeatedKeysBackIntoTheList_InOrder()
    {
        var text = "[TestSection]\nOverrides=first\nOverrides=second\nOverrides=third\n";
        var document = IniDocument.Parse(text);
        var settings = new ListSettings();

        IniSerializer.Apply(settings, IniFile.Game, document);

        Assert.Equal(new[] { "first", "second", "third" }, settings.Overrides);
    }

    [Fact]
    public void WriteThenApply_RoundTripsTheListExactly()
    {
        var original = new ListSettings();
        original.Overrides.Add("entry-one");
        original.Overrides.Add("entry-two");

        var text = IniSerializer.Write(original, IniFile.Game).ToString();
        var restored = new ListSettings();
        IniSerializer.Apply(restored, IniFile.Game, IniDocument.Parse(text));

        Assert.Equal(original.Overrides, restored.Overrides);
    }
}
