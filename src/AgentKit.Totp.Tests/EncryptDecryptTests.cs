using AgentKit.Totp.Storage;

namespace AgentKit.Totp.Tests;

public class EncryptDecryptTests
{
    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginalPlaintext()
    {
        var store = new EncryptedFileTotpStore();
        var plaintext = """{"Version":1,"Entries":{"test":{"Secret":"JBSWY3DPEHPK3PXP"}}}""";

        var encrypted = store.Encrypt(plaintext);
        var decrypted = store.Decrypt(encrypted);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_EmptyString_RoundTrips()
    {
        var store = new EncryptedFileTotpStore();
        var plaintext = "";

        var encrypted = store.Encrypt(plaintext);
        var decrypted = store.Decrypt(encrypted);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachTime()
    {
        var store = new EncryptedFileTotpStore();
        var plaintext = "test data";

        var encrypted1 = store.Encrypt(plaintext);
        var encrypted2 = store.Encrypt(plaintext);

        Assert.NotEqual(encrypted1, encrypted2);
    }
}
