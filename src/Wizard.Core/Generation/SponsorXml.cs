namespace Wizard.Core;

/// <summary>The maintenance fee declaration as it appears in the generated Directory.Build.props (plan 14).</summary>
public static class SponsorXml
{
    public const string FeeDocs = "https://github.com/VerifyTests/Verify/blob/main/docs/maintenance-fee.md";
    public const string SponsorCheckWizard = "https://simoncropp.github.io/SponsorCheck/package/Verify";
    public const string SponsorsPage = "https://github.com/sponsors/VerifyTests";

    /// <summary>The comment and property group, each line prefixed with <paramref name="indent"/>, ending with a newline.</summary>
    /// <param name="owners">Other SponsorCheck owners whose gates the selected packages bring in (plan A8).</param>
    public static string Block(WizardState state, string indent, IReadOnlyList<SponsorOwner>? owners = null)
    {
        owners ??= [];
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
            AppendUndecidedOwners(owners, Line);
            return builder.ToString();
        }

        Line($"<!-- {Explanation(state)} -->");
        foreach (var line in MsBuildXml.PropertyGroup(properties).Split('\n'))
        {
            Line(line);
        }

        AppendOtherOwners(state, owners, Line);
        return builder.ToString();
    }

    /// <summary>
    /// Some packages the wizard can emit depend on another package with a SponsorCheck gate of its own,
    /// under its own property prefix (plan A8). Without a declaration for each, the build fails with
    /// SC021 naming that owner.
    /// </summary>
    /// <remarks>
    /// An exemption and an opt-out are statements about the consumer, so they hold for every owner
    /// equally and are repeated. Sponsoring is not: a GitHub account sponsors one project, so that
    /// block is emitted commented out for the reader to fill in.
    /// </remarks>
    static void AppendOtherOwners(WizardState state, IReadOnlyList<SponsorOwner> owners, Action<string> line)
    {
        foreach (var owner in owners)
        {
            line("");
            line($"<!-- {owner.Package} carries the same check under its own prefix, for {owner.DisplayName}.");

            if (!Transfers(state, owner))
            {
                AppendOwnerUndecided(state, owner, line);
                continue;
            }

            line("     The declaration above is about this project rather than about any one package, so it");
            line("     is repeated here. -->");
            foreach (var text in MsBuildXml.PropertyGroup(SponsorRules.Properties(state, $"{owner.Prefix}_")).Split('\n'))
            {
                line(text);
            }
        }
    }

    /// <summary>
    /// Whether the declaration made for Verify says anything true about another owner. Opting out and a
    /// private arrangement are about this project, so they carry over. A sponsorship does not: a GitHub
    /// account sponsors one project. An exemption carries only when that owner offers the same one.
    /// </summary>
    public static bool Transfers(WizardState state, SponsorOwner owner) =>
        state.SponsorMode switch
        {
            SponsorMode.Exempt => owner.Accepts(state.Exemption),
            SponsorMode.PrivateArrangement => true,
            SponsorMode.Ignore => true,
            _ => false
        };

    static void AppendOwnerUndecided(WizardState state, SponsorOwner owner, Action<string> line)
    {
        switch (state.SponsorMode)
        {
            case SponsorMode.Exempt:
                line($"     {owner.DisplayName} does not offer the exemption claimed above, so it cannot be");
                line("     repeated here. The build fails with SC021 until one of these is chosen.");
                break;
            case SponsorMode.NotChosen:
                line("     The project's existing declaration uses the Verify_ prefix and does not cover this");
                line("     owner. The build fails with SC021 until one of these is chosen.");
                break;
            default:
                line("     A sponsorship is per project, so this one has to be decided separately. The build");
                line("     fails with SC021 until it is.");
                break;
        }

        line("     Uncomment and complete exactly one: -->");
        line("<!-- <PropertyGroup>");
        line($"       <{owner.Prefix}_GitHubSponsorAccount>your-github-account</{owner.Prefix}_GitHubSponsorAccount>");
        line("     </PropertyGroup>");
        foreach (var exemption in owner.Exemptions)
        {
            line("");
            var criteria = SponsorRules.Criteria(exemption);
            line($"     {char.ToUpperInvariant(criteria[0])}{criteria[1..]}:");
            line("     <PropertyGroup>");
            line($"       <{owner.Prefix}_SponsorshipExemption>{exemption}</{owner.Prefix}_SponsorshipExemption>");
            line($"       <{owner.Prefix}_SponsorshipExemptionUntil>yyyy-MM</{owner.Prefix}_SponsorshipExemptionUntil>");
            line("     </PropertyGroup>");
        }

        line("");
        line("     Or opt out, which logs a breach-of-license warning on every build:");
        line("     <PropertyGroup>");
        line($"       <{owner.Prefix}_SponsorshipLicenseIgnored>true</{owner.Prefix}_SponsorshipLicenseIgnored>");
        line("     </PropertyGroup> -->");
    }

    /// <summary>
    /// For a project that already declares its Verify status: only the owners the added packages bring
    /// in, each with a declaration that carries over or its options commented out.
    /// </summary>
    public static string OwnersOnly(WizardState state, string indent, IReadOnlyList<SponsorOwner> owners)
    {
        var builder = new StringBuilder();
        AppendOtherOwners(
            state,
            owners,
            text =>
            {
                if (text.Length == 0)
                {
                    builder.Append('\n');
                    return;
                }

                builder.Append(indent).Append(text).Append('\n');
            });
        return builder.ToString().TrimStart('\n');
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

    static void AppendUndecidedOwners(IReadOnlyList<SponsorOwner> owners, Action<string> line)
    {
        foreach (var owner in owners)
        {
            line("");
            line($"<!-- {owner.Package} carries the same check for {owner.DisplayName}, under the prefix");
            line($"     {owner.Prefix}_ instead of Verify_. It needs a declaration of its own too. -->");
        }
    }

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
