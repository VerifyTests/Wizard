namespace Wizard.Core;

public static class SolutionNames
{
    /// <summary>
    /// Keeps letters, digits, dots and underscores, so the name is safe as a folder, a project name and
    /// a namespace. Falls back to <see cref="WizardState.DefaultSolutionName"/> when nothing usable is left.
    /// </summary>
    public static string Clean(string? name)
    {
        var cleaned = new string((name ?? "").Where(_ => char.IsAsciiLetterOrDigit(_) || _ is '.' or '_').ToArray())
            .Trim('.');
        if (cleaned.Length == 0 || char.IsAsciiDigit(cleaned[0]))
        {
            return WizardState.DefaultSolutionName;
        }

        return cleaned;
    }

    /// <summary>
    /// The root namespace for a generated project (plan D17). The MTP entry point is generated into the
    /// root namespace; if its first segment is exactly <c>Verify</c>, that namespace shadows the
    /// <c>Verify()</c> method for tests in the global namespace. Only that segment is replaced, and not
    /// with <c>VerifyTests</c>, which is Verify's own namespace.
    /// </summary>
    public static string RootNamespace(string projectName)
    {
        if (projectName == "Verify")
        {
            return "Verification";
        }

        if (projectName.StartsWith("Verify.", StringComparison.Ordinal))
        {
            return "Verification" + projectName["Verify".Length..];
        }

        return projectName;
    }
}
