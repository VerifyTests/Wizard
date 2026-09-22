namespace Wizard.Core;

/// <summary>The maintenance fee declaration as it appears in the generated Directory.Build.props (plan 14).</summary>
public static class SponsorXml
{
    public const string FeeDocs = "https://github.com/VerifyTests/Verify/blob/main/docs/maintenance-fee.md";
    public const string SponsorCheckWizard = "https://simoncropp.github.io/SponsorCheck/package/Verify";
    public const string SponsorsPage = "https://github.com/sponsors/VerifyTests";

    /// <summary>The comment and property group, each line prefixed with <paramref name="indent"/>, ending with a newline.</summary>
    public static string Block(WizardState state, string indent)
    {
        var builder = new StringBuilder();
        void Line(string text = "")
        {
            if (text.Length == 0)
            {
                builder.Append('\n');
                return;
            }

            builder.Append(indent).Append(text).Append('\n');
        }

        Line("<!-- Open Source Maintenance Fee: every Verify package checks at build time that the build");
        Line("     declares its fee status, and fails with SC021 when it does not. Nothing phones home.");
        Line($"     {FeeDocs}");
        Line($"     Other options, and the rules for each: {SponsorCheckWizard} -->");

        var properties = SponsorRules.Properties(state);
        if (properties.Count == 0)
        {
            AppendAllOptionsCommented(Line);
            return builder.ToString();
        }

        Line($"<!-- {Explanation(state)} -->");
        foreach (var line in MsBuildXml.PropertyGroup(properties).Split('\n'))
        {
            Line(line);
        }

        return builder.ToString();
    }

    static string Explanation(WizardState state) =>
        state.SponsorMode switch
        {
            SponsorMode.Sponsor when state.SponsorshipPrivateUntil.Length > 0 =>
                "Sponsoring VerifyTests from this GitHub account, privately. A private sponsorship is never in the bundled sponsor list, so the claim is time-bounded: renew the month before it passes.",
            SponsorMode.Sponsor when state.SponsorshipStart != null =>
                "Sponsoring VerifyTests from this GitHub account. The sponsorship began after the referenced Verify version was packed, so it is not in that version's bundled sponsor list yet; the start date covers the gap and can be removed after upgrading Verify.",
            SponsorMode.Sponsor =>
                "Sponsoring VerifyTests from this GitHub account.",
            SponsorMode.Exempt =>
                $"Exempt: {SponsorRules.Criteria(state.Exemption ?? Exemption.OpenSource)}. The claim is time-bounded: renew the month before it passes.",
            SponsorMode.PrivateArrangement =>
                "A private licensing arrangement, covered until the end of this month. Renew it with a one-line edit.",
            SponsorMode.Ignore =>
                "Opting out. Every build logs an SC023 breach-of-license warning.",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state.SponsorMode, null)
        };

    static void AppendAllOptionsCommented(Action<string> line)
    {
        line("<!-- Not decided yet, so the build fails with SC021. Move exactly one of these out of this comment:");
        line("");
        line($"     Sponsoring VerifyTests ({SponsorsPage}) from a GitHub organization or user:");
        line("<PropertyGroup>");
        line("  <Verify_GitHubSponsorAccount>your-github-account</Verify_GitHubSponsorAccount>");
        line("</PropertyGroup>");
        line("");
        line("     Exempt (OpenSource, SmallRevenue or MaintainerConsulting), until a month at most 12 months");
        line("     out (6 for MaintainerConsulting):");
        line("<PropertyGroup>");
        line("  <Verify_SponsorshipExemption>OpenSource</Verify_SponsorshipExemption>");
        line("  <Verify_SponsorshipExemptionUntil>yyyy-MM</Verify_SponsorshipExemptionUntil>");
        line("</PropertyGroup>");
        line("");
        line("     A private licensing arrangement, until a month at most 12 months out:");
        line("<PropertyGroup>");
        line("  <Verify_SponsorshipLicensedUntil>yyyy-MM</Verify_SponsorshipLicensedUntil>");
        line("</PropertyGroup>");
        line("");
        line("     Opting out, which logs an SC023 breach-of-license warning on every build:");
        line("<PropertyGroup>");
        line("  <Verify_SponsorshipLicenseIgnored>true</Verify_SponsorshipLicenseIgnored>");
        line("</PropertyGroup>");
        line("-->");
    }
}
