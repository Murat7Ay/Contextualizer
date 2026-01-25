using System;
using System.Globalization;

namespace Contextualizer.Core.FunctionProcessing.FunctionExecutors
{
    internal static class DateTimeFunctionExecutor
    {
        public static object ProcessDateTimeMethod(DateTime dateTime, string methodName, string[] parameters)
        {
            return methodName.ToLower() switch
            {
                "add" => ProcessDateTimeAdd(dateTime, parameters),
                "subtract" => ProcessDateTimeSubtract(dateTime, parameters),
                "format" => ProcessDateTimeFormat(dateTime, parameters),
                _ => throw new NotSupportedException($"DateTime method '{methodName}' is not supported")
            };
        }

        private static DateTime ProcessDateTimeAdd(DateTime dateTime, string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Add method requires 2 parameters: unit and value");

            var unit = parameters[0].ToLower();
            if (!int.TryParse(parameters[1], out var value))
                throw new ArgumentException($"Invalid numeric value: {parameters[1]}");

            return unit switch
            {
                "days" or "day" => dateTime.AddDays(value),
                "hours" or "hour" => dateTime.AddHours(value),
                "minutes" or "minute" => dateTime.AddMinutes(value),
                "seconds" or "second" => dateTime.AddSeconds(value),
                "months" or "month" => dateTime.AddMonths(value),
                "years" or "year" => dateTime.AddYears(value),
                _ => throw new ArgumentException($"Unsupported time unit: {unit}")
            };
        }

        private static DateTime ProcessDateTimeSubtract(DateTime dateTime, string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Subtract method requires 2 parameters: unit and value");

            var unit = parameters[0].ToLower();
            if (!int.TryParse(parameters[1], out var value))
                throw new ArgumentException($"Invalid numeric value: {parameters[1]}");

            return unit switch
            {
                "days" or "day" => dateTime.AddDays(-value),
                "hours" or "hour" => dateTime.AddHours(-value),
                "minutes" or "minute" => dateTime.AddMinutes(-value),
                "seconds" or "second" => dateTime.AddSeconds(-value),
                "months" or "month" => dateTime.AddMonths(-value),
                "years" or "year" => dateTime.AddYears(-value),
                _ => throw new ArgumentException($"Unsupported time unit: {unit}")
            };
        }

        private static string ProcessDateTimeFormat(DateTime dateTime, string[] parameters)
        {
            if (parameters.Length == 0)
                return dateTime.ToString();

            var format = parameters[0];
            return dateTime.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
