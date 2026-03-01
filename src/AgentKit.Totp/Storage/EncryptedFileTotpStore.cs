using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AgentKit.Totp.Storage;

public class EncryptedFileTotpStore : ITotpStore
{
    private readonly string _storePath;
    private readonly string _saltPath;
    private const int Pbkdf2Iterations = 600_000;
    private const int KeySize = 32; // AES-256
    private const int NonceSize = 12; // AES-GCM nonce
    private const int TagSize = 16;  // AES-GCM tag

    public EncryptedFileTotpStore()
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".agentkit", "totp");
        Directory.CreateDirectory(baseDir);
        _storePath = Path.Combine(baseDir, "store.enc");
        _saltPath = Path.Combine(baseDir, ".salt");
    }

    private byte[] GetOrCreateSalt()
    {
        if (File.Exists(_saltPath))
            return File.ReadAllBytes(_saltPath);

        var salt = RandomNumberGenerator.GetBytes(32);
        File.WriteAllBytes(_saltPath, salt);
        // Restrict permissions on Unix
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_saltPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        return salt;
    }

    private string GetMachineId()
    {
        if (OperatingSystem.IsLinux())
        {
            var machineIdPath = "/etc/machine-id";
            if (File.Exists(machineIdPath))
                return File.ReadAllText(machineIdPath).Trim();
        }
        // Fallback: use username + machine name
        return $"{Environment.UserName}@{Environment.MachineName}";
    }

    internal byte[] DeriveKey()
    {
        var salt = GetOrCreateSalt();
        var machineId = GetMachineId();
        var password = Encoding.UTF8.GetBytes(machineId);
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeySize);
    }

    internal byte[] Encrypt(string plaintext)
    {
        var key = DeriveKey();
        try
        {
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            // Format: nonce (12) + tag (16) + ciphertext
            var result = new byte[NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);
            return result;
        }
        finally
        {
            Array.Clear(key);
        }
    }

    internal string Decrypt(byte[] data)
    {
        var key = DeriveKey();
        try
        {
            var nonce = data[..NonceSize];
            var tag = data[NonceSize..(NonceSize + TagSize)];
            var ciphertext = data[(NonceSize + TagSize)..];
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            Array.Clear(key);
        }
    }

    private async Task<Dictionary<string, TotpEntry>> ReadStoreAsync()
    {
        if (!File.Exists(_storePath))
            return new Dictionary<string, TotpEntry>();

        var encrypted = await File.ReadAllBytesAsync(_storePath);
        var json = Decrypt(encrypted);
        var store = JsonSerializer.Deserialize(json, StoreJsonContext.Default.StoreSchema);
        return store?.Entries ?? new Dictionary<string, TotpEntry>();
    }

    private async Task WriteStoreAsync(Dictionary<string, TotpEntry> entries)
    {
        var schema = new StoreSchema { Version = 1, Entries = entries };
        var json = JsonSerializer.Serialize(schema, StoreJsonContext.Default.StoreSchema);
        var encrypted = Encrypt(json);
        await File.WriteAllBytesAsync(_storePath, encrypted);
    }

    public async Task AddAsync(string name, TotpEntry entry, bool force = false)
    {
        var entries = await ReadStoreAsync();
        if (!force && entries.ContainsKey(name))
            throw new InvalidOperationException($"Entry '{name}' already exists. Use --force to overwrite.");
        entries[name] = entry;
        await WriteStoreAsync(entries);
    }

    public async Task<TotpEntry?> GetAsync(string name)
    {
        var entries = await ReadStoreAsync();
        return entries.TryGetValue(name, out var entry) ? entry : null;
    }

    public async Task<IReadOnlyList<string>> ListAsync()
    {
        var entries = await ReadStoreAsync();
        return entries.Keys.OrderBy(k => k).ToList();
    }

    public async Task RemoveAsync(string name)
    {
        var entries = await ReadStoreAsync();
        if (!entries.Remove(name))
            throw new KeyNotFoundException($"No entry found with name '{name}'");
        await WriteStoreAsync(entries);
    }

    public async Task<IReadOnlyDictionary<string, TotpEntry>> ExportAllAsync()
    {
        return await ReadStoreAsync();
    }
}
