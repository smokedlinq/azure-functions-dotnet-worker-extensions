using Microsoft.Azure.Functions.Worker.Converters;
using System.Security.Claims;

namespace Microsoft.Azure.Functions.Worker;

internal class ClaimsPrincipalConverter : IInputConverter
{
    public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        if (context.TargetType != typeof(ClaimsPrincipal))
        {
            return ValueTask.FromResult(ConversionResult.Unhandled());
        }

        try
        {
            var principal = context.FunctionContext.GetClaimsPrincipal();

            return principal is null
                ? ValueTask.FromResult(ConversionResult.Unhandled())
                : ValueTask.FromResult(ConversionResult.Success(principal));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(ConversionResult.Failed(ex));
        }
    }
}