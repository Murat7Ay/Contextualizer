using System;

namespace Contextualizer.Core.FunctionProcessing.FunctionExecutors
{
    internal static class MathFunctionExecutor
    {
        public static string ProcessMathFunction(string functionName, string[] parameters)
        {
            var mathFunction = functionName.Substring(5); // Remove "math." prefix

            return mathFunction.ToLower() switch
            {
                "add" => ProcessMathAdd(parameters),
                "subtract" => ProcessMathSubtract(parameters),
                "multiply" => ProcessMathMultiply(parameters),
                "divide" => ProcessMathDivide(parameters),
                "round" => ProcessMathRound(parameters),
                "floor" => ProcessMathFloor(parameters),
                "ceil" => ProcessMathCeil(parameters),
                "min" => ProcessMathMin(parameters),
                "max" => ProcessMathMax(parameters),
                "abs" => ProcessMathAbs(parameters),
                _ => throw new NotSupportedException($"Math function '{mathFunction}' is not supported")
            };
        }

        private static string ProcessMathAdd(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math add requires 2 parameters: number1, number2");

            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
                return (a + b).ToString();

            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathSubtract(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math subtract requires 2 parameters: number1, number2");

            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
                return (a - b).ToString();

            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathMultiply(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math multiply requires 2 parameters: number1, number2");

            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
                return (a * b).ToString();

            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathDivide(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math divide requires 2 parameters: number1, number2");

            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
            {
                if (b == 0)
                    throw new DivideByZeroException("Division by zero");
                return (a / b).ToString();
            }

            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathRound(string[] parameters)
        {
            if (parameters.Length < 1 || parameters.Length > 2)
                throw new ArgumentException("Math round requires 1-2 parameters: number, [digits]");

            if (!double.TryParse(parameters[0], out var number))
                throw new ArgumentException("Invalid numeric value");

            if (parameters.Length == 2)
            {
                if (!int.TryParse(parameters[1], out var digits))
                    throw new ArgumentException("Invalid digits value");
                return Math.Round(number, digits).ToString();
            }

            return Math.Round(number).ToString();
        }

        private static string ProcessMathFloor(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Math floor requires 1 parameter: number");

            if (double.TryParse(parameters[0], out var number))
                return Math.Floor(number).ToString();

            throw new ArgumentException("Invalid numeric value");
        }

        private static string ProcessMathCeil(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Math ceil requires 1 parameter: number");

            if (double.TryParse(parameters[0], out var number))
                return Math.Ceiling(number).ToString();

            throw new ArgumentException("Invalid numeric value");
        }

        private static string ProcessMathMin(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math min requires 2 parameters: number1, number2");

            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
                return Math.Min(a, b).ToString();

            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathMax(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math max requires 2 parameters: number1, number2");

            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
                return Math.Max(a, b).ToString();

            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathAbs(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Math abs requires 1 parameter: number");

            if (double.TryParse(parameters[0], out var number))
                return Math.Abs(number).ToString();

            throw new ArgumentException("Invalid numeric value");
        }
    }
}
