using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Options;
using Elmah.Io.Client;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Elmah.Io.Functions.Isolated.Test
{
    public class ElmahIoFunctionsMiddlewareTest
    {
        private FormatException innerException;
        private Exception outerException;

        [SetUp]
        public void SetUp()
        {
            innerException = new FormatException("Inner");
            outerException = new Exception("Outer", innerException);
        }

        [Test]
        public async Task CanLogException()
        {
            // Arrange
            var options = Options.Create(new ElmahIoFunctionOptions { ApiKey = "API_KEY", LogId = Guid.NewGuid() });
            var middleware = new ElmahIoFunctionsMiddleware(options);

            var elmahIoClient = Substitute.For<IElmahioAPI>();
            var messagesClient = Substitute.For<IMessagesClient>();
            elmahIoClient.Messages.Returns(messagesClient);

            MessageShipper.elmahIoClient = elmahIoClient;

            var functionContext = Substitute.For<FunctionContext>();

            // Act
            Assert.ThrowsAsync<Exception>(async () => await middleware.Invoke(functionContext, new FunctionExecutionDelegate(Hello)));

            // Assert
            await messagesClient
                .Received()
                .CreateAndNotifyAsync(
                    Arg.Is(options.Value.LogId),
                    Arg.Is<CreateMessage>(msg =>
                        msg.Title == "Inner"
                        && msg.DateTime.HasValue
                        && msg.Detail != null
                        && msg.Type == "System.FormatException"
                        && msg.Hostname != null
                        && msg.Severity == "Error"));
        }

        private Task Hello(FunctionContext context)
        {
            throw outerException;
        }
    }
}
