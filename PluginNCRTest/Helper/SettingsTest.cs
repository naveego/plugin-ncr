using System;
using PluginNCR.Helper;
using Xunit;

namespace PluginNCRTest.Helper
{
    public class SettingsTest
    {
        [Fact]
        public void ValidateValidTest()
        {
            // setup
            var settings = new Settings
            {
                ProvUsername = @"123",
                ProvPassword = @"123",
                NepApplicationKey = @"123",
                NepOrganization = @"123"
            };

            // act
            settings.Validate();

            // assert
        }
    }
}