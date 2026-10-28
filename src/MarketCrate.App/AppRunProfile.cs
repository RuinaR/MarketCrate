using System.IO;

namespace MarketCrate.App;

// This operator acknowledgement is NOT VM detection or a security boundary.
// Only the guest-local manual launcher may set it. Never launch this profile on the host.
public sealed record AppRunProfile(string DataRoot, bool OfflineQa)
{
    public const string GuestAcknowledgement = "dedicated-offline-windows-guest";
    public static AppRunProfile Create(string[] args, string localAppData, string? acknowledgement)
    {
        if (args.Length == 0) return new(Path.Combine(localAppData, "MarketCrate"), false);
        if (args.Length != 1 || args[0] != "--isolated-qa" || acknowledgement != GuestAcknowledgement)
            throw new ArgumentException("Unsupported or unacknowledged launch profile");
        return new(Path.Combine(localAppData, "MarketCrate", "IsolatedGuiQa"), true);
    }
}
