using Noundry.EnterpriseApiClient.Authentication;

namespace Noundry.EnterpriseApiClient.Tests.Authentication;

[TestFixture]
public class TokenAuthenticationProviderTests
{
    [Test]
    public async Task GetAccessTokenAsync_ReturnsConfiguredToken()
    {
        const string expectedToken = "test-token-123";
        var provider = new TokenAuthenticationProvider(expectedToken);

        var result = await provider.GetAccessTokenAsync();

        Assert.That(result, Is.EqualTo(expectedToken));
    }

    [Test]
    public async Task RefreshTokenAsync_DoesNotThrow()
    {
        var provider = new TokenAuthenticationProvider("test-token");

        Assert.DoesNotThrowAsync(async () => await provider.RefreshTokenAsync());
    }

    [Test]
    public void Constructor_NullToken_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TokenAuthenticationProvider(null!));
    }
}