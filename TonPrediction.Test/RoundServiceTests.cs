using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace TonPrediction.Test
{
    public class RoundServiceTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
    {
        /// <summary>
        /// 实例化容器
        /// </summary>
        private readonly IServiceProvider _serviceProvider = factory.Services;

        [Fact]
        public void Test1()
        {

        }
    }
}
