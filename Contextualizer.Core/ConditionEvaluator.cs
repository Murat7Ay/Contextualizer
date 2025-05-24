using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class ConditionEvaluator
    {
        public static bool EvaluateCondition(Condition? condition, ContextWrapper context)
        {
            if (condition is null)
            {
                return true;
            }

            if (condition.Operator == "and" || condition.Operator == "or")
            {
                if (condition.Conditions == null || condition.Conditions.Count == 0)
                    return false;

                bool result = condition.Operator == "and";

                foreach (var subCondition in condition.Conditions)
                {
                    bool evalResult = EvaluateCondition(subCondition, context);

                    if (condition.Operator == "and" && !evalResult) return false;
                    if (condition.Operator == "or" && evalResult) return true;
                }
                return result;
            }
            else
            {
                if (string.IsNullOrEmpty(condition.Field) || string.IsNullOrEmpty(condition.Value))
                    return false;

                string fieldValue;
                if (!context.TryGetValue(condition.Field, out fieldValue))
                    return false;

                string expectedValue = ResolveValue(condition.Value, context);
                return EvaluateOperator(fieldValue.ToString(), condition.Operator, expectedValue);
            }
        }

        private static string ResolveValue(string value, ContextWrapper context)
        {
            var match = Regex.Match(value, @"\$\(([^)]+)\)");
            if (match.Success)
            {
                string key = match.Groups[1].Value;
                return context.ContainsKey(key) ? context[key].ToString() : value;
            }
            return value;
        }

        private static bool EvaluateOperator(string fieldValue, string op, string expectedValue)
        {
            switch (op)
            {
                case "equals": return fieldValue == expectedValue;
                case "not_equals": return fieldValue != expectedValue;
                case "greater_than": return double.TryParse(fieldValue, out var fv) && double.TryParse(expectedValue, out var ev) && fv > ev;
                case "less_than": return double.TryParse(fieldValue, out fv) && double.TryParse(expectedValue, out ev) && fv < ev;
                case "contains": return fieldValue.Contains(expectedValue);
                case "starts_with": return fieldValue.StartsWith(expectedValue);
                case "ends_with": return fieldValue.EndsWith(expectedValue);
                case "matches_regex": return Regex.IsMatch(fieldValue, expectedValue);
                case "is_empty": return string.IsNullOrEmpty(fieldValue);
                case "is_not_empty": return !string.IsNullOrEmpty(fieldValue);
                default: throw new NotImplementedException($"Operator '{op}' is not implemented.");
            }
        }
    }
}
