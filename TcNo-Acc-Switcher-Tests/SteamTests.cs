using Xunit;
using TcNo_Acc_Switcher_Server.Pages.Steam;

namespace TcNo_Acc_Switcher_Tests
{
    public class SteamTests
    {
        [Theory]
        [InlineData("76561197960287930", true)]  // Valid Steam64 ID
        [InlineData("7656119796028793", false)]   // Too short
        [InlineData("765611979602879300", false)] // Too long
        [InlineData("abcdefghijklmnopq", false)] // Not digits
        [InlineData("0", false)]                  // Too small
        public void VerifySteamId_ValidatesCorrectIds(string steamId, bool expected)
        {
            // Act
            bool result = SteamSwitcherFuncs.VerifySteamId(steamId);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}