# agentkit-totp

A `dotnet tool` for managing TOTP (Time-based One-Time Password) secrets — designed for AI agents and humans alike.

## Install

```bash
dotnet tool install -g AgentKit.Totp
```

## Usage

```bash
# Add a TOTP entry from an otpauth:// URI
totp add github-cobalt --uri "otpauth://totp/GitHub:cobalt@example.com?secret=BASE32SECRET&issuer=GitHub"

# Add from a QR code image
totp add github-cobalt --qr /path/to/qrcode.png

# Add from a raw base32 secret
totp add github-cobalt --secret BASE32SECRET

# Get the current token
totp get github-cobalt

# Watch mode (live countdown)
totp get github-cobalt --watch

# List all entries
totp list

# Remove an entry
totp remove github-cobalt

# Export all secrets (encrypted backup)
totp export
```

## Design Goals

- **Agent-friendly**: simple CLI interface, scriptable, no interactive prompts required
- **Secure**: secrets encrypted at rest
- **Cross-platform**: works on Linux, macOS, Windows
- **Installable as a dotnet tool**: `dotnet tool install -g AgentKit.Totp`

## Built With

- [System.CommandLine](https://github.com/dotnet/command-line-api)
- [Otp.NET](https://github.com/kspearrin/Otp.NET)
- [QRCoder](https://github.com/codebude/QRCoder)

## License

MIT
