using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.Azure.Functions.Worker.Http
{
    public class JwtBearerAuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private OpenIdConnectConfiguration? _configuration;

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var authentication = GetAuthenticationAttribute(context);
            
            if (authentication is not null)
            {
                var request = context.GetHttpRequestData();

                if (request is not null)
                {
                    var principal = await AuthenticateAsync(context, request).ConfigureAwait(false);

                    if (principal is null)
                    {
                        context.SetHttpResponseData(request.CreateResponse(HttpStatusCode.Unauthorized));
                        return;
                    }
                    else
                    {
                        context.Items[nameof(ClaimsPrincipal)] = principal;

                        if (!authentication.IsAuthorized(principal))
                        {
                            context.SetHttpResponseData(request.CreateResponse(HttpStatusCode.Forbidden));
                            return;
                        }
                    }
                }
            }

            await next(context).ConfigureAwait(false);
        }

        private JwtBearerAuthenticationAttribute? GetAuthenticationAttribute(FunctionContext context)
            => context.FunctionDefinition.GetMethod().GetCustomAttribute<JwtBearerAuthenticationAttribute>();

        private async Task<ClaimsPrincipal?> AuthenticateAsync(FunctionContext context, HttpRequestData request)
        {
            var logger = context.GetLogger<JwtBearerAuthenticationMiddleware>();

            try
            {
                var options = context.InstanceServices.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().CurrentValue;

                if (_configuration is null && options.ConfigurationManager != null)
                {
                    _configuration = await options.ConfigurationManager.GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);
                }

                if (request.Headers.TryGetValues("Authorization", out var values))
                {
                    var authorization = values.SingleOrDefault();

                    if (authorization is not null)
                    {
                        var value = AuthenticationHeaderValue.Parse(authorization);

                        if (string.Equals(JwtBearerDefaults.AuthenticationScheme, value.Scheme, StringComparison.InvariantCultureIgnoreCase))
                        {
                            var token = value.Parameter;
                            var validationParameters = options.TokenValidationParameters.Clone();

                            if (_configuration is not null)
                            {
                                var issuers = new[] { _configuration.Issuer };
                                validationParameters.ValidIssuers = validationParameters.ValidIssuers?.Concat(issuers) ?? issuers;

                                validationParameters.IssuerSigningKeys = validationParameters.IssuerSigningKeys?.Concat(_configuration.SigningKeys)
                                    ?? _configuration.SigningKeys;
                            }

                            List<Exception>? validationFailures = null;
                            SecurityToken? validatedToken = null;
                            foreach (var validator in options.SecurityTokenValidators)
                            {
                                if (validator.CanReadToken(token))
                                {
                                    ClaimsPrincipal principal;
                                    try
                                    {
                                        principal = validator.ValidateToken(token, validationParameters, out validatedToken);
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.LogInformation(ex, "Token validation failed: {Message}", ex.Message);

                                        // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the event.
                                        if (options.RefreshOnIssuerKeyNotFound && options.ConfigurationManager != null
                                            && ex is SecurityTokenSignatureKeyNotFoundException)
                                        {
                                            options.ConfigurationManager.RequestRefresh();
                                        }

                                        if (validationFailures == null)
                                        {
                                            validationFailures = new List<Exception>(1);
                                        }
                                        validationFailures.Add(ex);
                                        continue;
                                    }

                                    logger.LogTrace("Token validation succeeded for principal: {Identity}", principal.Identity?.Name ?? "[null]");

                                    return principal;
                                }
                            }

                            if (validationFailures is not null)
                            {
                                var exception = (validationFailures.Count == 1) ? validationFailures[0] : new AggregateException(validationFailures);
                                logger.LogTrace(exception, "Token validation failed: {Message}", exception.Message);
                            }
                            else
                            {
                                logger.LogTrace("No SecurityTokenValidator available for token: {Token}", token ?? "[null]");
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Authentication failed: {Message}", ex.Message);
                throw;
            }
        }
    }
}