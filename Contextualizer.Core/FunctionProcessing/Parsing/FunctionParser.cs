using System.Collections.Generic;

namespace Contextualizer.Core.FunctionProcessing
{
    internal static class FunctionParser
    {
        public static string ProcessRegularFunctions(string input, Dictionary<string, string>? context = null)
        {
            return Parsing.RegularFunctionParser.ProcessRegularFunctions(input, context);
        }

        public static string ProcessPipelineFunctions(string input, Dictionary<string, string>? context = null)
        {
            return Parsing.PipelineFunctionParser.ProcessPipelineFunctions(input, context);
        }
    }
}
