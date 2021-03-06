﻿using System.Threading;
using System.Threading.Tasks;
using Flurl.Http.Testing;
using Glasswall.PolicyManagement.Business.Services;
using Glasswall.PolicyManagement.Common.Configuration;
using Glasswall.PolicyManagement.Common.Models;
using Glasswall.PolicyManagement.Common.Models.Adaption;
using Glasswall.PolicyManagement.Common.Models.Adaption.ContentFlags;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TestCommon;

namespace PolicyManagement.Business.Tests.Services.PolicyDistributerTests.Distribute
{
    [TestFixture]
    public class WhenNoEndpointsConfigured : UnitTestBase<PolicyDistributer>
    {
        private Mock<ILogger<PolicyDistributer>> _logger;
        private Mock<IPolicyManagementApiConfiguration> _configuration;
        private PolicyModel _input;
        private CancellationToken _token;

        private HttpTest _httpTest;

        [OneTimeSetUp]
        public async Task Setup()
        {
            _logger = new Mock<ILogger<PolicyDistributer>>();
            _configuration = new Mock<IPolicyManagementApiConfiguration>();

            ClassInTest = new PolicyDistributer(_logger.Object, _configuration.Object);

            _configuration.Setup(s => s.PolicyUpdateServiceEndpointCsv)
                .Returns("");

            _httpTest = new HttpTest();

            await ClassInTest.Distribute(_input = new PolicyModel { AdaptionPolicy = new AdaptionPolicy
            {
                ContentManagementFlags = new ContentFlags()
            }}, _token = new CancellationToken());

        }

        [OneTimeTearDown]
        public void Teardown()
        {
            _httpTest.Dispose();
        }

        [Test]
        public void Each_Endpoint_Invokes_Put()
        {
            Assert.That(_httpTest.CallLog.Count, Is.EqualTo(0));
        }
    }
}
