
using Template.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace TemplateTests.ControllerTests
{
    public class TestControllerTests : TestHelperBase
    {

        public TestControllerTests()
        {
            var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        }

        [Fact]
        public void Test()
        {
            // Arrange

            // Act

            // Assert
        }
    }
}
