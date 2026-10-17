using Xunit;

namespace AnimCancel.Tests;

/// <summary>
/// The API guard tests return quietly when the game assemblies cannot be found, so
/// that the suite still runs on a machine without Valheim. The cost of that is a
/// green run that proves nothing about API compatibility, with no way to tell the
/// two apart from the output.
///
/// This test closes that hole: on a normal development machine it fails loudly if
/// the guards are inert. Set ANIMCANCEL_ALLOW_MISSING_GAME=1 to allow a run
/// without Valheim, which is the only situation where a silent pass is acceptable.
/// </summary>
public class GuardsAreArmedTests
{
    [Fact]
    public void Game_assemblies_were_found_so_the_api_guards_actually_ran()
    {
        if (GameAssembly.IsAvailable)
        {
            return;
        }

        string? allowed = System.Environment.GetEnvironmentVariable("ANIMCANCEL_ALLOW_MISSING_GAME");
        if (allowed == "1")
        {
            return;
        }

        Assert.Fail(
            "assembly_valheim.dll was not found, so all API guard "
            + "tests passed without asserting anything. Set VALHEIM_MANAGED to your "
            + "valheim_Data\\Managed folder, or set ANIMCANCEL_ALLOW_MISSING_GAME=1 to "
            + "acknowledge that this run does not check API compatibility.");
    }
}
