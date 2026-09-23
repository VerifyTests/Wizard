/// <summary>
/// The Open Source Maintenance Fee declaration (plan 14, Verify's docs/maintenance-fee.md). Verify
/// uses SponsorCheck owner mode with owner id <c>Verify</c>, so the declaration is MSBuild properties
/// prefixed <c>Verify_</c>. Diagnostic codes are the owner-mode ones from SponsorCheck's consumer wizard.
/// </summary>
public static class SponsorRules
{
    public const string Prefix = "Verify_";
    public const int PrivateMaxMonths = 12;
    public const int LicensedMaxMonths = 12;

    static Regex gitHubAccount = new("^[A-Za-z0-9](?:[A-Za-z0-9-]{0,38})$");

    public static int MaxMonths(Exemption exemption)
    {
        if (exemption == Exemption.MaintainerConsulting)
        {
            return 6;
        }

        return 12;
    }

    public static string Criteria(Exemption exemption) =>
        exemption switch
        {
            Exemption.OpenSource => "an open source project that does not generate revenue",
            Exemption.SmallRevenue => "an individual, or an organization with annual gross revenue under US$10,000 (government agencies excluded)",
            Exemption.MaintainerConsulting => "an organization that engaged the core maintainers for consulting work, for six months from the end of that engagement",
            _ => throw new ArgumentOutOfRangeException(nameof(exemption), exemption, null)
        };

    public static string? Summary(WizardState state) =>
        state.SponsorMode switch
        {
            SponsorMode.Sponsor when state.SponsorAccount.Length > 0 => $"Sponsor: {state.SponsorAccount}",
            SponsorMode.Exempt when state.Exemption != null => $"Exempt: {state.Exemption}",
            // An existing project already declares its status; leaving it alone is the usual answer.
            SponsorMode.NotChosen when state.Flow != Flow.New => "No change",
            _ => state.SponsorMode.Name()
        };

    /// <summary>Fills the dates a mode needs with the plan 14 defaults, leaving values already set.</summary>
    public static void ApplyDefaults(WizardState state, Date today)
    {
        switch (state.SponsorMode)
        {
            case SponsorMode.Exempt:
                state.Exemption ??= Exemption.OpenSource;
                if (state.SponsorUntil.Length == 0)
                {
                    state.SponsorUntil = MonthBound.Ceiling(today, MaxMonths(state.Exemption.Value));
                }

                break;
            case SponsorMode.PrivateArrangement:
                if (state.SponsorUntil.Length == 0)
                {
                    state.SponsorUntil = MonthBound.Ceiling(today, LicensedMaxMonths);
                }

                break;
        }
    }

    public static IReadOnlyList<string> Errors(WizardState state, Date today)
    {
        var errors = new List<string>();
        switch (state.SponsorMode)
        {
            case SponsorMode.Sponsor:
                if (!gitHubAccount.IsMatch(state.SponsorAccount))
                {
                    errors.Add("Enter the GitHub account (organization or user) the sponsorship is made from.");
                }

                if (state.SponsorshipStart > today)
                {
                    errors.Add("The sponsorship start cannot be in the future: the build fails with SC028.");
                }

                if (state.SponsorshipPrivateUntil.Length > 0)
                {
                    AddMonthErrors(errors, "The private sponsorship end month", state.SponsorshipPrivateUntil, today, PrivateMaxMonths);
                }

                break;
            case SponsorMode.Exempt:
                if (state.Exemption == null)
                {
                    errors.Add("Choose the exemption that applies.");
                    break;
                }

                AddMonthErrors(errors, "The exemption end month", state.SponsorUntil, today, MaxMonths(state.Exemption.Value));
                break;
            case SponsorMode.PrivateArrangement:
                AddMonthErrors(errors, "The licensed-until month", state.SponsorUntil, today, LicensedMaxMonths);
                break;
        }

        return errors;
    }

    static void AddMonthErrors(List<string> errors, string label, string value, Date today, int maxMonths)
    {
        if (!MonthBound.TryParse(value, out _, out _))
        {
            errors.Add($"{label} must be a month in the form yyyy-MM.");
            return;
        }

        if (MonthBound.IsExpired(value, today))
        {
            errors.Add($"{label} has already passed.");
            return;
        }

        if (MonthBound.IsBeyondCeiling(value, today, maxMonths))
        {
            errors.Add($"{label} can be at most {maxMonths} months out ({MonthBound.Ceiling(today, maxMonths)}).");
        }
    }

    /// <summary>The <c>Verify_*</c> properties for the chosen mode; empty for <see cref="SponsorMode.NotChosen"/>.</summary>
    /// <param name="prefix">
    /// The owner's property prefix. Defaults to Verify's; another owner's gate reads the same property
    /// names under its own prefix (plan A8).
    /// </param>
    public static IReadOnlyList<(string Name, string Value)> Properties(WizardState state, string prefix = Prefix)
    {
        switch (state.SponsorMode)
        {
            case SponsorMode.Sponsor:
                var properties = new List<(string Name, string Value)>
                {
                    ("GitHubSponsorAccount", state.SponsorAccount)
                };
                // A private declaration decides the build outright, so a start date alongside it is
                // never read (SponsorCheck's consumer wizard drops it for the same reason).
                if (state.SponsorshipPrivateUntil.Length > 0)
                {
                    properties.Add(("SponsorshipPrivateUntil", state.SponsorshipPrivateUntil));
                }
                else if (state.SponsorshipStart is { } start)
                {
                    properties.Add(("SponsorshipStart", start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                }

                return Prefixed(properties, prefix);
            case SponsorMode.Exempt:
                return Prefixed(
                [
                    ("SponsorshipExemption", state.Exemption?.ToString() ?? ""),
                    ("SponsorshipExemptionUntil", state.SponsorUntil)
                ],
                    prefix);
            case SponsorMode.PrivateArrangement:
                return Prefixed([("SponsorshipLicensedUntil", state.SponsorUntil)], prefix);
            case SponsorMode.Ignore:
                return Prefixed([("SponsorshipLicenseIgnored", "true")], prefix);
            default:
                return [];
        }
    }

    static IReadOnlyList<(string Name, string Value)> Prefixed(List<(string Name, string Value)> properties, string prefix) =>
        properties.Select(_ => (prefix + _.Name, _.Value)).ToList();

    /// <summary>What the first build does with the declaration, in the words the guide uses.</summary>
    public static string Outcome(WizardState state) =>
        state.SponsorMode switch
        {
            SponsorMode.Sponsor when state.SponsorshipPrivateUntil.Length > 0 =>
                $"Private sponsorships are never in the bundled sponsor list, so the declaration is trusted: the build passes through the end of {state.SponsorshipPrivateUntil} (UTC) and logs an SC059 audit message. After that it fails with SC058 until the month is renewed; a month more than {PrivateMaxMonths} months out fails with SC055.",
            SponsorMode.Sponsor when state.SponsorshipStart != null =>
                "When the start date is after the referenced Verify version was packed, the build passes and logs an SC017 audit message. A future date fails with SC028. After upgrading to a Verify version packed later than the start date, the normal bundled-list check applies and `Verify_SponsorshipStart` can be removed.",
            SponsorMode.Sponsor =>
                "The build passes silently when the account is in the sponsor list bundled with the referenced Verify version. If it fails with SC024, the sponsorship either began after that version was packed (add `Verify_SponsorshipStart`) or is private on GitHub (use `Verify_SponsorshipPrivateUntil`).",
            SponsorMode.Exempt =>
                $"The build passes and logs the exemption's criteria as an SC031 message, not a warning, so builds with warnings as errors pass too. It passes through the end of {state.SponsorUntil} (UTC); after that it fails with SC049 until the claim is renewed.",
            SponsorMode.PrivateArrangement =>
                $"The build passes silently through the end of {state.SponsorUntil} (UTC). After that it fails with SC025 until the month is renewed; a month more than {LicensedMaxMonths} months out fails with SC037.",
            SponsorMode.Ignore =>
                "The build passes but logs an SC023 breach-of-license warning on every build, so a build with warnings as errors fails. The generated `Directory.Build.props` does not treat warnings as errors for that reason.",
            _ =>
                "No declaration is made yet, so the first build fails with SC021. Its message contains a copy-pasteable fix, and `Directory.Build.props` contains every option, commented out."
        };
}
