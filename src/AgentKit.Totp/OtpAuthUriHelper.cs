using System.Web;
using AgentKit.Totp.Storage;

namespace AgentKit.Totp;

public static class OtpAuthUriHelper
{
    public static TotpEntry? ParseOtpAuthUri(string uri)
    {
        if (!uri.StartsWith("otpauth://totp/", StringComparison.OrdinalIgnoreCase)) return null;
        var u = new Uri(uri);
        var query = HttpUtility.ParseQueryString(u.Query);
        var secret = query["secret"];
        if (string.IsNullOrWhiteSpace(secret)) return null;
        var path    = Uri.UnescapeDataString(u.AbsolutePath.TrimStart('/'));
        var issuer  = query["issuer"] ?? (path.Contains(':') ? path.Split(':', 2)[0] : null);
        var account = path.Contains(':') ? path.Split(':', 2)[1] : path;
        _ = int.TryParse(query["digits"], out var digits); if (digits == 0) digits = 6;
        _ = int.TryParse(query["period"], out var period); if (period == 0) period = 30;
        var algorithm = query["algorithm"]?.ToUpperInvariant() ?? "SHA1";
        return new TotpEntry(secret.ToUpperInvariant().Replace(" ", ""), issuer, account, digits, period, algorithm);
    }
}
