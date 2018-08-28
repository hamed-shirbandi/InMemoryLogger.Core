using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;

namespace InMemoryLogger.Core
{
    /// <summary>
    /// Enables the MVC logging service.
    /// </summary>
    public class IMCaptureMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMLoggerOptions _options;
        private readonly ILogger _logger;

        public IMCaptureMiddleware(RequestDelegate next, ILoggerFactory factory, IOptions<IMLoggerOptions> options)
        {
            _next = next;
            _options = options.Value;
            _logger = factory.CreateLogger<IMCaptureMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            using (RequestIdentifier.Ensure(context))
            {
                var requestId = context.Features.Get<IHttpRequestIdentifierFeature>().TraceIdentifier;
                using (_logger.BeginScope("Request: {RequestId}", requestId))
                {
                    try
                    {
                        IMLoggerScope.Current.Context.HttpInfo = GetHttpInfo(context);
                        await _next(context);
                    }
                    finally
                    {
                        IMLoggerScope.Current.Context.HttpInfo.StatusCode = context.Response.StatusCode;
                    }
                }
            }
        }




        /// <summary>
        /// Takes the info from the given HttpContext and copies it to an HttpInfo object
        /// </summary>
        /// <returns>The HttpInfo for the current mvc logger context</returns>
        private static HttpInfo GetHttpInfo(HttpContext context)
        {
            return new HttpInfo()
            {
                RequestID = context.Features.Get<IHttpRequestIdentifierFeature>().TraceIdentifier,
                Host = context.Request.Host,
                ContentType = context.Request.ContentType,
                Path = context.Request.Path,
                Scheme = context.Request.Scheme,
                StatusCode = context.Response.StatusCode,
                User = context.User,
                Method = context.Request.Method,
                Protocol = context.Request.Protocol,
                Headers = context.Request.Headers,
                Query = context.Request.QueryString,
                Cookies = context.Request.Cookies
            };
        }
    }

    public class HttpInfo
    {
        public string RequestID { get; set; }

        public HostString Host { get; set; }

        public PathString Path { get; set; }

        public string ContentType { get; set; }

        public string Scheme { get; set; }

        public int StatusCode { get; set; }

        public ClaimsPrincipal User { get; set; }

        public string Method { get; set; }

        public string Protocol { get; set; }

        public IHeaderDictionary Headers { get; set; }

        public QueryString Query { get; set; }

        public IRequestCookieCollection Cookies { get; set; }
    }

    internal class RequestIdentifier : IDisposable
    {
        private readonly bool _addedFeature;
        private readonly bool _updatedIdentifier;
        private readonly string _originalIdentifierValue;
        private readonly HttpContext _context;
        private readonly IHttpRequestIdentifierFeature _feature;

        private RequestIdentifier(HttpContext context)
        {
            _context = context;
            _feature = context.Features.Get<IHttpRequestIdentifierFeature>();

            if (_feature == null)
            {
                _feature = new HttpRequestIdentifierFeature()
                {
                    TraceIdentifier = Guid.NewGuid().ToString()
                };
                context.Features.Set(_feature);
                _addedFeature = true;
            }
            else if (string.IsNullOrEmpty(_feature.TraceIdentifier))
            {
                _originalIdentifierValue = _feature.TraceIdentifier;
                _feature.TraceIdentifier = Guid.NewGuid().ToString();
                _updatedIdentifier = true;
            }
        }

        public static IDisposable Ensure(HttpContext context)
        {
            return new RequestIdentifier(context);
        }

        public void Dispose()
        {
            if (_addedFeature)
            {
                _context.Features.Set<IHttpRequestIdentifierFeature>(null);
            }
            else if (_updatedIdentifier)
            {
                _feature.TraceIdentifier = _originalIdentifierValue;
            }
        }
    }

   
}
