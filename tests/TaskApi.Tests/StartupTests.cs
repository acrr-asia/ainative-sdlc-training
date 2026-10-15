using Microsoft.Extensions.Options;

namespace TaskApi.Tests;

public class StartupTests
{
    [Fact]
    public void App_refuses_to_start_with_a_weak_signing_key()
    {
        // spec S1
        using var api = new ApiFactory { SigningKeyOverride = "too-short" };

        var ex = Assert.ThrowsAny<Exception>(() => api.CreateClient());

        Assert.IsType<OptionsValidationException>(ex.GetBaseException());
    }
}
