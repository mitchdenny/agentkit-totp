using AgentKit.Totp.Storage;

namespace AgentKit.Totp.Tests;

public class AddOverwriteTests
{
    [Fact]
    public async Task AddAsync_DuplicateName_ThrowsWithoutForce()
    {
        var store = new EncryptedFileTotpStore();
        var entry = new TotpEntry("JBSWY3DPEHPK3PXP");
        var name = $"test-dup-{Guid.NewGuid():N}";

        await store.AddAsync(name, entry);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.AddAsync(name, entry));
        Assert.Contains("already exists", ex.Message);

        // Cleanup
        await store.RemoveAsync(name);
    }

    [Fact]
    public async Task AddAsync_DuplicateName_SucceedsWithForce()
    {
        var store = new EncryptedFileTotpStore();
        var entry1 = new TotpEntry("JBSWY3DPEHPK3PXP");
        var entry2 = new TotpEntry("KRSXG5CTMVRXEZLU");
        var name = $"test-force-{Guid.NewGuid():N}";

        await store.AddAsync(name, entry1);
        await store.AddAsync(name, entry2, force: true);

        var result = await store.GetAsync(name);
        Assert.Equal("KRSXG5CTMVRXEZLU", result!.Secret);

        // Cleanup
        await store.RemoveAsync(name);
    }
}
