using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Elmah.Io.Functions.Isolated.TimerTrigger
{
    public static class Function1
    {
        [Function("Function1")]
        public static void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, FunctionContext context)
        {
            var logger = context.GetLogger("Function1");
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");

            throw new Exception("An error happened");
        }
    }
}
