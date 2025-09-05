
using EncompassAPI.Controllers;
using EncompassAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace EncompassAPITests.ControllerTests
{
    public class DisclosuresControllerTests : TestHelperBase
    {
        DisclosuresController thisController;

        public DisclosuresControllerTests()
        {
            var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            thisController = new DisclosuresController(scopeFactory);
        }

        [Fact]
        public void Test()
        {
            // Arrange
            CandidIncomingRequest request = new CandidIncomingRequest()
            {
                Path = "/test/x/",
                OrgId = "00D4x000007swJU",
                LoanId = "b90983c3-b851-449b-832a-30b72e9b1175",
                LoId = "0054x000005rWC9AAM",
                DocumentId = "a0FKY000003rwXO2AY"
            };

            // Act
            var actionResult = thisController.RetrieveDisclosures(request);
            var objectResult = actionResult as ObjectResult;
            AcceptedResult response = objectResult.Value as AcceptedResult;

            // Assert
        }
    }
}
