using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Http
{
    internal static class FunctionContextExtensions
    {
        // https://github.com/Azure/azure-functions-dotnet-worker/issues/414
        public static HttpRequestData? GetHttpRequestData(this FunctionContext context)
        {
            var feature = context.Features.SingleOrDefault(f => f.Key.Name == "IFunctionBindingsFeature").Value;
            Type type = feature.GetType();
            var inputData = type.GetProperties().Single(p => p.Name == "InputData").GetValue(feature) as IReadOnlyDictionary<string, object>;
            return inputData?.Values.SingleOrDefault(o => o is HttpRequestData) as HttpRequestData;
        }

        public static void SetHttpResponseData(this FunctionContext context, HttpResponseData response)
        {
            var feature = context.Features.SingleOrDefault(f => f.Key.Name == "IFunctionBindingsFeature").Value;
            Type type = feature.GetType();
            var result = type.GetProperties().Single(p => p.Name == "InvocationResult");
            result.SetValue(feature, response);
        }
    }
}
