using Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Extenstion
{
    public static class HttpTransactionLoggerExtensions
    {
        public static IServiceCollection AddHttpTransactionLogger(this IServiceCollection services, Action<HttpTransactionLoggerOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureOptions);

            services.Configure(configureOptions);
            services.AddSingleton<ILoggerService, LoggerService>();

            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetService<ILoggerService>();

            if (service != null)
            {
                try
                {
                    service.InitTable();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }

            return services;
        }

        public static IApplicationBuilder UseHttpTransactionLogger(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.UseMiddleware<RequestResponseLoggingMiddleware>();
            return app;
        }
    }
}
