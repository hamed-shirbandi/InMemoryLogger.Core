using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using InMemoryLogger.Core.Pages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Collections.Generic;

namespace InMemoryLogger.Core
{
    /// <summary>
    /// Enables viewing logs captured by the <see cref="IMCaptureMiddleware"/>.
    /// </summary>
    public class IMLoggerPageMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMLoggerOptions _options;
        private readonly IMLoggerStore _store;

        public IMLoggerPageMiddleware(RequestDelegate next, IOptions<IMLoggerOptions> options, IMLoggerStore store)
        {
            _next = next;
            _options = options.Value;
            _store = store;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments(_options.Path))
            {
                await _next(context);
                return;
            }

            var t = await ParseParams(context);
            var options = t.Item1;
            var redirect = t.Item2;
            if (redirect)
            {
                return;
            }
            if (context.Request.Path == _options.Path)
            {
                RenderMainLogPage(options, context);
            }
            else
            {
                RenderDetailsPage(options, context);
            }
        }

        private void RenderMainLogPage(IMLoggerViewOptions options, HttpContext context)
        {
            var activities = _store.GetActivities()
                .Where(it => it.Root != null && HasAnyRelevantMessage(it.Root, _options));
            var model = new IMLogPageModel()
            {
                Activities = activities,
                Options = options,
                Path = _options.Path
            };
            var logPage = new LogPageBuilder(model);

            logPage.Execute(context);
        }




        private bool HasAnyRelevantMessage(IMLoggerScopeNode node, IMLoggerOptions options)
        {
            var hasAnyRelevantMessage = node.Messages.Count > 0 && node.Messages.Any(ms => options.Filter("any", ms.Severity));
            
            foreach (var nd in node.Children)
            {
                hasAnyRelevantMessage = hasAnyRelevantMessage | HasAnyRelevantMessage(nd, options);
            }

            return hasAnyRelevantMessage;
        }




        private void RenderDetailsPage(IMLoggerViewOptions options, HttpContext context)
        {
            var parts = context.Request.Path.Value.Split('/');
            var id = Guid.Empty;
            if (!Guid.TryParse(parts[parts.Length - 1], out id))
            {
                context.Response.StatusCode = 400;
                //await context.Response.WriteAsync("Invalid Id");
                return;
            }
            var model = new IMDetailsPageModel()
            {
                Activity = _store.GetActivities().Where(a => a.Id == id).FirstOrDefault(),
                Options = options
            };
            if (model.Activity == null)
            {
                model.Activity = _store.GetActivities().FirstOrDefault();
                if (model.Activity.Root == null)
                {
                    model.Activity.Root = new IMLoggerScopeNode()
                    {
                        StartTime = DateTimeOffset.UtcNow,
                        State = new object(),
                        Name = "no name"
                    };
                }
            }
            var detailsPage = new DetailsPageBuilder(model);
            detailsPage.Execute(context);
        }

        private async Task<Tuple<IMLoggerViewOptions, bool>> ParseParams(HttpContext context)
        {
            var options = new IMLoggerViewOptions()
            {
                MinLevel = LogLevel.Debug,
                NamePrefix = string.Empty
            };
            var isRedirect = false;

            IFormCollection form = null;
            if (context.Request.HasFormContentType)
            {
                form = await context.Request.ReadFormAsync();
            }

            if (form != null && form.ContainsKey("clear"))
            {
                _store.Clear();
                context.Response.Redirect(context.Request.PathBase.Add(_options.Path).ToString());
                isRedirect = true;
            }
            else
            {
                if (context.Request.Query.ContainsKey("level"))
                {
                    var minLevel = options.MinLevel;
                    if (Enum.TryParse<LogLevel>(context.Request.Query["level"], out minLevel))
                    {
                        options.MinLevel = minLevel;
                    }
                }
                if (context.Request.Query.ContainsKey("name"))
                {
                    var namePrefix = context.Request.Query["name"];
                    options.NamePrefix = namePrefix;
                }
            }
            return Tuple.Create(options, isRedirect);
        }
    }

    public class IMLoggerViewOptions
    {
        /// <summary>
        /// The minimum <see cref="LogLevel"/> of logs shown on the mvc logger page.
        /// </summary>
        public LogLevel MinLevel { get; set; }

        /// <summary>
        /// The prefix for the logger names of logs shown on the mvc logger page.
        /// </summary>
        public string NamePrefix { get; set; }
    }

    public class IMLoggerScopeNode
    {
        public IMLoggerScopeNode Parent { get; set; }

        public List<IMLoggerScopeNode> Children { get; private set; } = new List<IMLoggerScopeNode>();

        public List<LogInfo> Messages { get; private set; } = new List<LogInfo>();

        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public object State { get; set; }

        public string Name { get; set; }

    }
}
