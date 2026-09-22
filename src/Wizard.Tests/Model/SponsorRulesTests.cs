using Wizard.Tests.Generation;

namespace Wizard.Tests.Model;

public class SponsorRulesTests
{
    static IReadOnlyList<string> Errors(WizardState state) =>
        SponsorRules.Errors(state, GeneratorTests.Today);

    [Test]
    public Task Validation() =>
        Verify(
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["sponsor, no account"] = Errors(new() {SponsorMode = SponsorMode.Sponsor}),
                ["sponsor, bad account"] = Errors(new() {SponsorMode = SponsorMode.Sponsor, SponsorAccount = "-acme"}),
                ["sponsor, future start"] = Errors(new() {SponsorMode = SponsorMode.Sponsor, SponsorAccount = "acme", SponsorshipStart = new Date(2026, 10, 1)}),
                ["sponsor, private too far"] = Errors(new() {SponsorMode = SponsorMode.Sponsor, SponsorAccount = "acme", SponsorshipPrivateUntil = "2027-10"}),
                ["sponsor, valid"] = Errors(new() {SponsorMode = SponsorMode.Sponsor, SponsorAccount = "acme", SponsorshipStart = GeneratorTests.Today}),
                ["exempt, none chosen"] = Errors(new() {SponsorMode = SponsorMode.Exempt}),
                ["exempt, expired"] = Errors(new() {SponsorMode = SponsorMode.Exempt, Exemption = Exemption.OpenSource, SponsorUntil = "2026-08"}),
                ["exempt, current month"] = Errors(new() {SponsorMode = SponsorMode.Exempt, Exemption = Exemption.OpenSource, SponsorUntil = "2026-09"}),
                ["exempt, consulting past 6 months"] = Errors(new() {SponsorMode = SponsorMode.Exempt, Exemption = Exemption.MaintainerConsulting, SponsorUntil = "2027-04"}),
                ["exempt, open source 12 months"] = Errors(new() {SponsorMode = SponsorMode.Exempt, Exemption = Exemption.OpenSource, SponsorUntil = "2027-09"}),
                ["private arrangement, bad format"] = Errors(new() {SponsorMode = SponsorMode.PrivateArrangement, SponsorUntil = "Sept 2027"}),
                ["decide later"] = Errors(new()),
                ["ignore"] = Errors(new() {SponsorMode = SponsorMode.Ignore})
            });

    [Test]
    public async Task DefaultsFillTheMaximumTerm()
    {
        var exempt = new WizardState {SponsorMode = SponsorMode.Exempt};
        SponsorRules.ApplyDefaults(exempt, GeneratorTests.Today);
        await Assert.That(exempt.Exemption).IsEqualTo(Exemption.OpenSource);
        await Assert.That(exempt.SponsorUntil).IsEqualTo("2027-09");

        var consulting = new WizardState {SponsorMode = SponsorMode.Exempt, Exemption = Exemption.MaintainerConsulting};
        SponsorRules.ApplyDefaults(consulting, GeneratorTests.Today);
        await Assert.That(consulting.SponsorUntil).IsEqualTo("2027-03");
    }
}
