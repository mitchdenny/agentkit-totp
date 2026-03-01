using System.CommandLine;
using System.Text;
using System.Web;
using AgentKit.Totp.Storage;
using OtpNet;
using ZXing;
using ZXing.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

var store = new EncryptedFileTotpStore();
var rootCommand = new RootCommand("agentkit-totp: TOTP secret management for AI agents and humans");

// ── totp add ──────────────────────────────────────────────────────────────────
var addCommand = new Command("add", "Add a new TOTP entry");
var addName    = new Argument<string>("name") { Description = "Name for this entry (e.g. github-cobalt)" };
var addUri     = new Option<string?>("--uri")    { Description = "otpauth:// URI" };
var addSecret  = new Option<string?>("--secret") { Description = "Base32-encoded TOTP secret" };
var addQr      = new Option<FileInfo?>("--qr")   { Description = "Path to QR code image file" };
addCommand.Add(addName);
addCommand.Add(addUri);
addCommand.Add(addSecret);
addCommand.Add(addQr);
addCommand.SetAction(async (parseResult) =>
{
    var name   = parseResult.GetValue(addName)!;
    var uri    = parseResult.GetValue(addUri);
    var secret = parseResult.GetValue(addSecret);
    var qrFile = parseResult.GetValue(addQr);

    TotpEntry? entry = null;

    if (uri != null)
    {
        entry = ParseOtpAuthUri(uri);
        if (entry == null) { Console.Error.WriteLine("Invalid otpauth:// URI."); return; }
    }
    else if (qrFile != null)
    {
        if (!qrFile.Exists) { Console.Error.WriteLine($"File not found: {qrFile.FullName}"); return; }
        var decoded = DecodeQrCode(qrFile.FullName);
        if (decoded == null) { Console.Error.WriteLine("Could not decode QR code from image."); return; }
        entry = ParseOtpAuthUri(decoded);
        if (entry == null) { Console.Error.WriteLine($"QR code did not contain a valid otpauth:// URI. Got: {decoded}"); return; }
    }
    else if (secret != null)
    {
        entry = new TotpEntry(secret.ToUpperInvariant().Replace(" ", ""));
    }
    else
    {
        Console.Error.WriteLine("Provide --uri, --secret, or --qr.");
        return;
    }

    try { Base32Encoding.ToBytes(entry.Secret); }
    catch { Console.Error.WriteLine("Invalid base32 secret."); return; }

    await store.AddAsync(name, entry);
    Console.WriteLine($"✓ Added '{name}'");
});

// ── totp get ──────────────────────────────────────────────────────────────────
var getCommand = new Command("get", "Generate the current TOTP token");
var getName    = new Argument<string>("name") { Description = "Name of the entry" };
var getWatch   = new Option<bool>("--watch") { Description = "Continuously output tokens with countdown" };
getCommand.Add(getName);
getCommand.Add(getWatch);
getCommand.SetAction(async (parseResult) =>
{
    var name  = parseResult.GetValue(getName)!;
    var watch = parseResult.GetValue(getWatch);

    var entry = await store.GetAsync(name);
    if (entry == null) { Console.Error.WriteLine($"No entry found: '{name}'"); return; }

    var keyBytes = Base32Encoding.ToBytes(entry.Secret);
    var totp = new OtpNet.Totp(keyBytes, entry.Period, OtpHashMode.Sha1, entry.Digits);

    if (!watch)
    {
        Console.WriteLine(totp.ComputeTotp());
        return;
    }

    Console.CancelKeyPress += (_, e) => { e.Cancel = false; };
    while (true)
    {
        var token = totp.ComputeTotp();
        var remaining = totp.RemainingSeconds();
        Console.Write($"\r{token}  ({remaining}s remaining)   ");
        await Task.Delay(1000);
        if (remaining <= 1) Console.WriteLine();
    }
});

// ── totp list ─────────────────────────────────────────────────────────────────
var listCommand = new Command("list", "List all stored TOTP entry names");
listCommand.SetAction(async (_) =>
{
    var names = await store.ListAsync();
    if (!names.Any()) { Console.WriteLine("(no entries)"); return; }
    foreach (var n in names) Console.WriteLine(n);
});

// ── totp remove ───────────────────────────────────────────────────────────────
var removeCommand = new Command("remove", "Remove a TOTP entry");
var removeName    = new Argument<string>("name") { Description = "Name of the entry to remove" };
removeCommand.Add(removeName);
removeCommand.SetAction(async (parseResult) =>
{
    var name = parseResult.GetValue(removeName)!;
    try { await store.RemoveAsync(name); Console.WriteLine($"✓ Removed '{name}'"); }
    catch (KeyNotFoundException e) { Console.Error.WriteLine(e.Message); }
});

// ── totp export ───────────────────────────────────────────────────────────────
var exportCommand = new Command("export", "Export all TOTP entries as otpauth:// URIs");
var exportOutput  = new Option<FileInfo?>("--output") { Description = "Output file path (default: stdout)" };
exportCommand.Add(exportOutput);
exportCommand.SetAction(async (parseResult) =>
{
    var output  = parseResult.GetValue(exportOutput);
    var entries = await store.ExportAllAsync();
    var sb = new StringBuilder();
    foreach (var (name, entry) in entries.OrderBy(e => e.Key))
        sb.AppendLine(BuildOtpAuthUri(name, entry));

    if (output != null)
    {
        await File.WriteAllTextAsync(output.FullName, sb.ToString());
        Console.WriteLine($"✓ Exported {entries.Count} entries to {output.FullName}");
    }
    else Console.Write(sb);
});

rootCommand.Add(addCommand);
rootCommand.Add(getCommand);
rootCommand.Add(listCommand);
rootCommand.Add(removeCommand);
rootCommand.Add(exportCommand);

return await rootCommand.Parse(args).InvokeAsync();

// ── Helpers ───────────────────────────────────────────────────────────────────
static TotpEntry? ParseOtpAuthUri(string uri)
{
    if (!uri.StartsWith("otpauth://totp/", StringComparison.OrdinalIgnoreCase)) return null;
    var u = new Uri(uri);
    var query = HttpUtility.ParseQueryString(u.Query);
    var secret = query["secret"];
    if (string.IsNullOrWhiteSpace(secret)) return null;
    var path    = Uri.UnescapeDataString(u.AbsolutePath.TrimStart('/'));
    var issuer  = query["issuer"] ?? (path.Contains(':') ? path.Split(':')[0] : null);
    var account = path.Contains(':') ? path.Split(':')[1] : path;
    _ = int.TryParse(query["digits"], out var digits); if (digits == 0) digits = 6;
    _ = int.TryParse(query["period"], out var period); if (period == 0) period = 30;
    return new TotpEntry(secret.ToUpperInvariant().Replace(" ", ""), issuer, account, digits, period);
}

static string BuildOtpAuthUri(string name, TotpEntry entry)
{
    var label = Uri.EscapeDataString(entry.Issuer != null ? $"{entry.Issuer}:{entry.Account ?? name}" : name);
    var sb = new StringBuilder($"otpauth://totp/{label}?secret={entry.Secret}");
    if (entry.Issuer != null) sb.Append($"&issuer={Uri.EscapeDataString(entry.Issuer)}");
    if (entry.Digits != 6)   sb.Append($"&digits={entry.Digits}");
    if (entry.Period != 30)  sb.Append($"&period={entry.Period}");
    return sb.ToString();
}

static string? DecodeQrCode(string imagePath)
{
    try
    {
        var reader = new ZXing.ImageSharp.BarcodeReader<Rgba32>();
        using var image = Image.Load<Rgba32>(imagePath);
        var result = reader.Decode(image);
        return result?.Text;
    }
    catch { return null; }
}
