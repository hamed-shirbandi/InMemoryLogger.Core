using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace InMemoryLogger.Core.Pages
{
    public class IMLogPageModel
    {
        public IEnumerable<ActivityContext> Activities { get; set; }

        public IMLoggerViewOptions Options { get; set; }

        public PathString Path { get; set; }
    }
}