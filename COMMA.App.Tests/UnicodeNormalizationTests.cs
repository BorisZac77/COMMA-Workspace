using System.Reflection;
using System.Text;
using COMMA.App.ViewModels;

namespace COMMA.App.Tests;

public sealed class UnicodeNormalizationTests
{
    [Fact]
    public void NfcAndNfdProductNames_NormalizeToTheSameFormCIdentity()
    {
        const string nfc = "Koszula męska krótki rękaw";
        var nfd = nfc.Normalize(NormalizationForm.FormD);

        Assert.NotEqual(nfc, nfd);

        var normalizedNfc = InvokeNormalizeProductIdentity(nfc);
        var normalizedNfd = InvokeNormalizeProductIdentity(nfd);

        Assert.Equal(nfc, normalizedNfc);
        Assert.Equal(normalizedNfc, normalizedNfd);
        Assert.True(normalizedNfd.IsNormalized(NormalizationForm.FormC));
    }

    private static string InvokeNormalizeProductIdentity(string value)
    {
        var method = typeof(MainViewModel).GetMethod(
            "NormalizeProductIdentity",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return Assert.IsType<string>(
            method.Invoke(null, [value]));
    }
}
