using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace InMemoryLogger.Core
{
    public class IMLoggerOptions
    {
        /// <summary>
        /// Specifies the path to view the logs.
        /// </summary>
        public PathString Path { get; set; } = new PathString("/IMLogs");


        /// <summary>
        /// Determines whether log statements should be logged based on the name of the logger
        /// and the <see cref="M:LogLevel"/> of the message.
        /// </summary>
        public Func<string, LogLevel, bool> Filter { get; set; } = (name, level) => true;
    }
}
