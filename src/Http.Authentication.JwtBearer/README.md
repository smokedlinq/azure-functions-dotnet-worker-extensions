# Azure Function App - .NET Worker - JwtBearer Authentication Middleware

Enable JwtBearer authentication for HttpTrigger requests.

## Usage

Example setup, configuration, and usage with an AzureAD (v2.0) JWT bearer protected endpoint.

> Note: Ensure the application manifest property `accessTokenAcceptedVersion` is set to `2`.

---

**`Program.cs`**

```csharp
.ConfigureFunctionsWorkerDefaults(builder =>
{
    // Use for AzureAD Authentication
    builder.AddJwtBearerAuthentication(options =>
    {
        options.TokenValidationParameters.NameClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
    });
})
```

---

**`local.settings.json`**

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "Authentication__MetadataAddress": "https://login.microsoftonline.com/{TENANT_ID}/v2.0/.well-known/openid-configuration",
    "Authentication__Audience": "{APPLICATION_ID}"
  }
}
```

---

**`HttpFunction.cs`**

```csharp
[Function(nameof(CreatePost))]
[JwtBearerAuthentication(Roles = "role1,role2")]
public HttpResponseData RunAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, 
    FunctionContext context)
{
    // Get the claims principal for use within the request
    var principal = context.GetClaimsPrincipal();

    return req.CreateResponse(HttpStatusCode.OK);
}
```