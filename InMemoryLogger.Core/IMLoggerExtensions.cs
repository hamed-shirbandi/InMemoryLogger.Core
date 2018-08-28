using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace InMemoryLogger.Core
{
    /// <summary>
    /// Extension methods for setting up MVC logger services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class IMLoggerExtensions
    {



        /// <summary>
        /// Adds error logging middleware services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">An <see cref="Action{MVCLoggerOptions}"/> to configure the provided <see cref="IMLoggerOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddInMemoryLogger(this IServiceCollection services, Action<IMLoggerOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            services.TryAddSingleton<IMLoggerStore>();
            services.TryAddSingleton<IMLoggerProvider>();
            services.Configure(setupAction);

            return services;
        }




        /// <summary>
        /// Enables the MVC logger service, which can be accessed via the <see cref=" IMLoggerPageMiddleware"/>.
        /// Enables viewing logs captured by the <see cref="IMCaptureMiddleware"/>.
        /// </summary>
        public static IApplicationBuilder UseInMemoryLogger(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            // add the mvc logger provider to the factory here so the logger can start capturing logs immediately
            var factory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var provider = app.ApplicationServices.GetRequiredService<IMLoggerProvider>();
            factory.AddProvider(provider);

             app.UseMiddleware<IMCaptureMiddleware>();
             app.UseMiddleware<IMLoggerPageMiddleware>();

            return app;
        }
        

    }
}
