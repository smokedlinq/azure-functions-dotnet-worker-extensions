using System;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.Azure.Functions.Worker.Http
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class JwtBearerAuthenticationAttribute : Attribute
    {
        public JwtBearerAuthenticationAttribute()
        {
        }

        public string? Roles { get; set; }

        public bool IsAuthorized(ClaimsPrincipal principal)
        {
            var values = Roles?.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return values is null || !values.Any() || values.Any(role => principal.IsInRole(role));
        }
    }
}