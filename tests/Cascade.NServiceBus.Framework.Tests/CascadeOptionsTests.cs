using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cascade.NServiceBus.Framework;

namespace Cascade.NServiceBus.Framework.Tests
{
    [TestClass]
    public class CascadeOptionsTests
    {
        [TestMethod]
        public void DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var options = new CascadeOptions();

            // Assert
            Assert.AreEqual("http://localhost:5100", options.CollectorUrl);
            Assert.IsNull(options.EndpointName);
            Assert.IsNull(options.HostId);
            Assert.IsTrue(options.IncludeHeaders);
            Assert.AreEqual(1000, options.BufferSize);
        }

        [TestMethod]
        public void Properties_CanBeSet()
        {
            // Arrange
            var options = new CascadeOptions
            {
                CollectorUrl = "http://example.com:8080",
                EndpointName = "TestEndpoint",
                HostId = "test-host",
                IncludeHeaders = false,
                BufferSize = 500
            };

            // Assert
            Assert.AreEqual("http://example.com:8080", options.CollectorUrl);
            Assert.AreEqual("TestEndpoint", options.EndpointName);
            Assert.AreEqual("test-host", options.HostId);
            Assert.IsFalse(options.IncludeHeaders);
            Assert.AreEqual(500, options.BufferSize);
        }
    }
}
