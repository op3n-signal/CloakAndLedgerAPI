using EncompassAPI.Controllers;
using EncompassAPI.Models.Configs;
using EncompassAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncompassAPITests.ControllerTests
{
    public class SettingsControllerTests : TestHelperBase
    {
        private SettingsController thisController;

        public SettingsControllerTests()
        {
            var loggerService = ServiceProvider.GetRequiredService<LoggerService<SettingsController>>();
            var awsOptions = ServiceProvider.GetRequiredService<IOptionsSnapshot<AWSConfig>>();
            thisController = new SettingsController(awsOptions, loggerService);
        }

        [Fact]
        public async void Test()
        {
            // Arrange
            string instanceId = "be11350238";
            var credentials = new EncompassCredentials
            {
                client_id = "9ikamp2",
                client_secret = "cqwTMbpQHgoA3ikKbXomFW6b@m0gR9*NLA7w6gS*QMLJXCHNy*8UoxkJBAFOv5AI",
                org_id = "00D4x000007swJU",
            };

            // Act
            IActionResult actionResult = await thisController.AddSecret(instanceId, credentials);
            
            // Assert 
           // actionResult.st
        }
    }
}
