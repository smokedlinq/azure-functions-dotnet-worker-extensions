using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker
{
    public static class JwtExtensions
    {
        public static IFunctionsWorkerApplicationBuilder AddJwtBearerAuthentication(this IFunctionsWorkerApplicationBuilder builder, Action<JwtBearerOptions>? configure = null)
        {
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            builder.Services
                .AddOptions<JwtBearerOptions>()
                .Configure<IConfiguration>((options, configuration) =>
                {
                    configuration.GetSection("Authentication").Bind(options);
                    configure?.Invoke(options);
                });

            builder.UseMiddleware<JwtBearerAuthenticationMiddleware>();

            return builder;
        }

        public static ClaimsPrincipal GetClaimsPrincipal(this FunctionContext context)
            => context.Items.TryGetValue(nameof(ClaimsPrincipal), out var value) ? (ClaimsPrincipal)value : new ClaimsPrincipal();
    }
}