using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Http
{
    internal static class MethodLocator
    {
        // https://github.com/Azure/azure-functions-dotnet-worker/blob/main/src/DotNetWorker.Core/Invocation/DefaultMethodInfoLocator.cs
        private static readonly Regex _entryPointRegex = new Regex("^(?<typename>.*)\\.(?<methodname>\\S*)$");

        public static MethodInfo GetMethod(this FunctionDefinition definition)
        {
            var pathToAssembly = definition.PathToAssembly;
            var entryPoint = definition.EntryPoint;
            var entryPointMatch = _entryPointRegex.Match(entryPoint);

            if (!entryPointMatch.Success)
            {
                throw new InvalidOperationException("Invalid entry point configuration. The function entry point must be defined in the format <fulltypename>.<methodname>");
            }

            string typeName = entryPointMatch.Groups["typename"].Value;
            string methodName = entryPointMatch.Groups["methodname"].Value;

            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(pathToAssembly);

            Type? functionType = assembly.GetType(typeName);

            MethodInfo? methodInfo = functionType?.GetMethod(methodName);

            if (methodInfo == null)
            {
                throw new InvalidOperationException($"Method '{methodName}' specified in {nameof(FunctionDefinition.EntryPoint)} was not found. This function cannot be created.");
            }

            return methodInfo;
        }
    }
}
