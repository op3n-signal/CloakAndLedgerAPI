using EncompassAPI.Models.Encompass;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncompassAPITests.UnitTests
{
    public class SimpleTests
    {
        [Fact]
        public void TestTakeWhile()
        {
            // Arrange
            List<DisclosureTrackingLog> disclosureTrackingLogs = new List<DisclosureTrackingLog>();
            disclosureTrackingLogs.Add(new DisclosureTrackingLog() { disclosureType = "Revised" });
            disclosureTrackingLogs.Add(new DisclosureTrackingLog() { disclosureType = "Revised" });
            disclosureTrackingLogs.Add(new DisclosureTrackingLog() { disclosureType = "Revised" });
            disclosureTrackingLogs.Add(new DisclosureTrackingLog() { disclosureType = "Initial" });
            disclosureTrackingLogs.Add(new DisclosureTrackingLog() { disclosureType = "Revised" });
            IEnumerable<DisclosureTrackingLog> ieDisclosureTrackingLogs = disclosureTrackingLogs;

            // Act
            ieDisclosureTrackingLogs = ieDisclosureTrackingLogs.TakeWhile(d => d.disclosureType.ToLower() == "revised");

            int count = ieDisclosureTrackingLogs.Count();

            // Assert
            count.Should().Be(3);
        }
    }
}
