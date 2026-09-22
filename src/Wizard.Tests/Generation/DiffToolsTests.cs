using DiffEngine;

namespace Wizard.Tests.Generation;

/// <summary>
/// The wizard lists diff tools from a static copy, because DiffEngine is not meant for WASM. This keeps
/// the copy equal to DiffEngine's own list: non-MDI tools that support the OS, in DiffEngine's order.
/// </summary>
public class DiffToolsTests
{
    [Test]
    [MatrixDataSource]
    public async Task MatchDiffEngine(Os os)
    {
        var expected = Definitions.Tools
            .Where(_ => !_.IsMdi && Supports(_, os))
            .Select(_ => $"{_.Tool} {_.Url}");
        var actual = Wizard.Core.DiffTools.For(os).Select(_ => $"{_.Name} {_.Url}");
        await Assert.That(actual).IsEquivalentTo(expected, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    static bool Supports(Definition definition, Os os) =>
        os switch
        {
            Os.Windows => definition.OsSupport.Windows != null,
            Os.MacOS => definition.OsSupport.Osx != null,
            _ => definition.OsSupport.Linux != null
        };
}
