namespace AgentKit.Totp.Storage;

public record TotpEntry(
    string Secret,
    string? Issuer = null,
    string? Account = null,
    int Digits = 6,
    int Period = 30,
    string Algorithm = "SHA1"
);
