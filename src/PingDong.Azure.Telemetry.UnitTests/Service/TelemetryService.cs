using System;
using Xunit;

namespace PingDong.Azure.Telemetry.UnitTests
{
    public class TelemetryServiceTests
    {
        [Fact]
        public void TelemetryService_Throw_IfClientIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new TelemetryService(null));
        }
    }
}
