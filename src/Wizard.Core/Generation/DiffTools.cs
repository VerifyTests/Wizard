namespace Wizard.Core;

public sealed record DiffTool(string Name, string Url);

/// <summary>
/// The non-MDI diff tools DiffEngine supports per OS, in DiffEngine's order: what the old wizard
/// listed. DiffEngine itself starts processes, so it is not referenced here (it is not meant for WASM);
/// a test compares these lists with DiffEngine.Definitions.Tools instead (plan 17.2).
/// </summary>
public static class DiffTools
{
    static DiffTool msWordDiff = new("MsWordDiff", "https://github.com/SimonCropp/MsOfficeDiff");
    static DiffTool msExcelDiff = new("MsExcelDiff", "https://github.com/SimonCropp/MsOfficeDiff");
    static DiffTool beyondCompare = new("BeyondCompare", "https://www.scootersoftware.com");
    static DiffTool p4Merge = new("P4Merge", "https://www.perforce.com/products/helix-core-apps/merge-diff-tool-p4merge");
    static DiffTool kaleidoscope = new("Kaleidoscope", "https://kaleidoscope.app");
    static DiffTool deltaWalker = new("DeltaWalker", "https://www.deltawalker.com/");
    static DiffTool winMerge = new("WinMerge", "https://winmerge.org/");
    static DiffTool tortoiseMerge = new("TortoiseMerge", "https://tortoisesvn.net/TortoiseMerge.html");
    static DiffTool tortoiseGitMerge = new("TortoiseGitMerge", "https://tortoisegit.org/docs/tortoisegitmerge/");
    static DiffTool tortoiseGitIDiff = new("TortoiseGitIDiff", "https://tortoisegit.org/docs/tortoisegitmerge/");
    static DiffTool tortoiseIDiff = new("TortoiseIDiff", "https://tortoisesvn.net/TortoiseIDiff.html");
    static DiffTool kDiff3 = new("KDiff3", "https://github.com/KDE/kdiff3");
    static DiffTool tkDiff = new("TkDiff", "https://sourceforge.net/projects/tkdiff/");
    static DiffTool guiffy = new("Guiffy", "https://www.guiffy.com/");
    static DiffTool examDiff = new("ExamDiff", "https://www.prestosoft.com/edp_examdiffpro.asp");
    static DiffTool diffinity = new("Diffinity", "https://truehumandesign.se/s_diffinity.php");
    static DiffTool rider = new("Rider", "https://www.jetbrains.com/rider/");
    static DiffTool vim = new("Vim", "https://www.vim.org/");
    static DiffTool neovim = new("Neovim", "https://neovim.io/");
    static DiffTool diffEngineViewer = new("DiffEngineViewer", "https://github.com/VerifyTests/DiffEngine");

    public static IReadOnlyList<DiffTool> For(Os os) =>
        os switch
        {
            Os.Windows =>
            [
                msWordDiff,
                msExcelDiff,
                beyondCompare,
                p4Merge,
                deltaWalker,
                winMerge,
                tortoiseMerge,
                tortoiseGitMerge,
                tortoiseGitIDiff,
                tortoiseIDiff,
                kDiff3,
                guiffy,
                examDiff,
                diffinity,
                rider,
                vim,
                neovim,
                diffEngineViewer
            ],
            Os.MacOS =>
            [
                beyondCompare,
                p4Merge,
                kaleidoscope,
                deltaWalker,
                kDiff3,
                tkDiff,
                guiffy,
                rider,
                vim,
                neovim,
                diffEngineViewer
            ],
            Os.Linux =>
            [
                beyondCompare,
                p4Merge,
                rider,
                neovim,
                diffEngineViewer
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(os), os, null)
        };
}
