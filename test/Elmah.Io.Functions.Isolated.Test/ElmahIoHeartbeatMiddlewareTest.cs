using Elmah.Io.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elmah.Io.Functions.Isolated.Test
{
    public class ElmahIoHeartbeatMiddlewareTest
    {
        private ElmahIoHeartbeatMiddleware middleware;
        private FunctionContext functionContext;
        private IHeartbeatsClient heartbeatsClient;
        private IOptions<ElmahIoFunctionOptions> options;

        [SetUp]
        public void SetUp()
        {
            options = Options.Create(new ElmahIoFunctionOptions { ApiKey = "API_KEY", LogId = Guid.NewGuid(), HeartbeatId = "HEARTBEAT_ID" });
            middleware = new ElmahIoHeartbeatMiddleware(options);

            var elmahIoClient = Substitute.For<IElmahioAPI>();
            heartbeatsClient = Substitute.For<IHeartbeatsClient>();
            elmahIoClient.Heartbeats.Returns(heartbeatsClient);

            middleware.api = elmahIoClient;

            functionContext = Substitute.For<FunctionContext>();
        }

        [Test]
        public async Task CanLogFail()
        {
            // Arrange

            // Act
            Assert.ThrowsAsync<Exception>(async () => await middleware.Invoke(functionContext, new FunctionExecutionDelegate(No)));

            // Assert
            await heartbeatsClient
                .Received()
                .UnhealthyAsync(
                    Arg.Is(options.Value.LogId),
                    Arg.Is(options.Value.HeartbeatId),
                    Arg.Is<string>(s => s.Contains("Exception")),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<long?>(),
                    Arg.Any<List<Check>>(),
                    Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task CanLogSucces()
        {
            // Arrange

            // Act
            await middleware.Invoke(functionContext, new FunctionExecutionDelegate(Yes));

            // Assert
            await heartbeatsClient
                .Received()
                .HealthyAsync(
                    Arg.Is(options.Value.LogId),
                    Arg.Is(options.Value.HeartbeatId),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<long?>(),
                    Arg.Any<List<Check>>(),
                    Arg.Any<CancellationToken>());
        }

        private static Task Yes(FunctionContext context)
        {
            return Task.CompletedTask;
        }

        private static Task No(FunctionContext context)
        {
            throw new Exception("No");
        }
    }
}
