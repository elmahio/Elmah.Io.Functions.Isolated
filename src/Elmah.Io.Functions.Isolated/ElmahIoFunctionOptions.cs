using Elmah.Io.Client;
using System;

namespace Elmah.Io.Functions.Isolated
{
    /// <summary>
    /// A range of properties to use when configuring Elmah.Io.Functions.Isolated.
    /// </summary>
    public class ElmahIoFunctionOptions
    {
        /// <summary>
        /// The API key from the elmah.io UI.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// The id of the log to send messages to.
        /// </summary>
        public Guid LogId { get; set; }

        /// <summary>
        /// The id of the heartbeat to send messages to.
        /// </summary>
        public string HeartbeatId { get; set; }

        /// <summary>
        /// An application name to put on all error messages.
        /// </summary>
        public string Application { get; set; }

        /// <summary>
        /// Configure the timeout to use when communicating with the elmah.io API. Default is 5 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Register an action to be called before logging an error. Use the OnMessage action to
        /// decorate error messages with additional information.
        /// </summary>
        public Action<CreateMessage> OnMessage { get; set; }

        /// <summary>
        /// Register an action to be called if communicating with the elmah.io API fails.
        /// You can use this callback to log the error through Microsoft.Extensions.Logging
        /// or what ever logging framework you may use.
        /// </summary>
        public Action<CreateMessage, Exception> OnError { get; set; }

        /// <summary>
        /// Register an action to filter log messages. Use this to add client-side ignore
        /// of some error messages. If the filter action returns true, the error is ignored.
        /// </summary>
        public Func<CreateMessage, bool> OnFilter { get; set; }
    }
}
