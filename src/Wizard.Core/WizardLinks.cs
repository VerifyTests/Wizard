namespace Wizard.Core;

/// <summary>
/// Canonical urls into the deployed wizard. One place so the generators, the docs and the tests
/// can't drift. It lives in Core because generated files link back to the wizard state they came from.
/// </summary>
public static class WizardLinks
{
    public const string Base = "https://verifytests.github.io/Wizard";
    public const string New = Base + "/new";
    public const string Add = Base + "/add";
    public const string AddByTech = Base + "/add/by-tech";

    /// <summary>
    /// The add flow seeded with one extension, for extension readmes to link to (plan D11).
    /// </summary>
    public static string AddExtension(string extensionId) =>
        $"{Add}/{Uri.EscapeDataString(extensionId.Trim())}";
}
