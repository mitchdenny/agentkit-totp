using System.CommandLine;

var rootCommand = new RootCommand("agentkit-totp: TOTP secret management for AI agents and humans");

// totp add <name> --uri <otpauth://...> | --secret <base32> | --qr <path>
var addCommand = new Command("add", "Add a new TOTP entry");
var addName = new Argument<string>("name", "Name for this TOTP entry (e.g. github-cobalt)");
var addUri = new Option<string?>("--uri", "otpauth:// URI (from QR code)");
var addSecret = new Option<string?>("--secret", "Base32-encoded TOTP secret");
var addQr = new Option<FileInfo?>("--qr", "Path to QR code image file");
addCommand.AddArgument(addName);
addCommand.AddOption(addUri);
addCommand.AddOption(addSecret);
addCommand.AddOption(addQr);
addCommand.SetAction((parseResult) =>
{
    Console.WriteLine("TODO: implement add");
});

// totp get <name> [--watch]
var getCommand = new Command("get", "Generate the current TOTP token for an entry");
var getName = new Argument<string>("name", "Name of the TOTP entry");
var getWatch = new Option<bool>("--watch", "Continuously output tokens with countdown");
getCommand.AddArgument(getName);
getCommand.AddOption(getWatch);
getCommand.SetAction((parseResult) =>
{
    Console.WriteLine("TODO: implement get");
});

// totp list
var listCommand = new Command("list", "List all stored TOTP entries");
listCommand.SetAction((parseResult) =>
{
    Console.WriteLine("TODO: implement list");
});

// totp remove <name>
var removeCommand = new Command("remove", "Remove a TOTP entry");
var removeName = new Argument<string>("name", "Name of the TOTP entry to remove");
removeCommand.AddArgument(removeName);
removeCommand.SetAction((parseResult) =>
{
    Console.WriteLine("TODO: implement remove");
});

// totp export
var exportCommand = new Command("export", "Export all TOTP secrets (encrypted)");
exportCommand.SetAction((parseResult) =>
{
    Console.WriteLine("TODO: implement export");
});

rootCommand.AddCommand(addCommand);
rootCommand.AddCommand(getCommand);
rootCommand.AddCommand(listCommand);
rootCommand.AddCommand(removeCommand);
rootCommand.AddCommand(exportCommand);

return await rootCommand.InvokeAsync(args);
