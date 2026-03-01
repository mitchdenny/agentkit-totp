namespace AgentKit.Totp.Storage;

public interface ITotpStore
{
    Task AddAsync(string name, TotpEntry entry);
    Task<TotpEntry?> GetAsync(string name);
    Task<IReadOnlyList<string>> ListAsync();
    Task RemoveAsync(string name);
    Task<IReadOnlyDictionary<string, TotpEntry>> ExportAllAsync();
}
