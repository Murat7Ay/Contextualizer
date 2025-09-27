using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class RegexHandler : Dispatch, IHandler
    {
        private readonly Regex _compiledRegex;
        
        // Constants for error handling
        private const string INVALID_REGEX_PATTERN = "Invalid regex pattern";
        private const string REGEX_MATCH_FAILED = "Regex match failed";

        public static string TypeName => "Regex";
        protected override string OutputFormat => base.HandlerConfig.OutputFormat;
        public new HandlerConfig HandlerConfig => base.HandlerConfig;

        public RegexHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            try
            {
                // Compile regex with optimizations for better performance
                _compiledRegex = new Regex(
                    handlerConfig.Regex, 
                    RegexOptions.Compiled | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(5) // 5 second timeout to prevent ReDoS attacks
                );
            }
            catch (ArgumentException ex)
            {
                UserFeedback.ShowError($"RegexHandler '{handlerConfig.Name}': {INVALID_REGEX_PATTERN} - {ex.Message}");
                throw new InvalidOperationException($"Invalid regex pattern in handler '{handlerConfig.Name}': {handlerConfig.Regex}", ex);
            }
            catch (RegexMatchTimeoutException ex)
            {
                UserFeedback.ShowError($"RegexHandler '{handlerConfig.Name}': Regex compilation timeout - {ex.Message}");
                throw new InvalidOperationException($"Regex compilation timeout in handler '{handlerConfig.Name}': {handlerConfig.Regex}", ex);
            }
        }

        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            if (clipboardContent?.Text == null)
                return false;

            try
            {
                return _compiledRegex.IsMatch(clipboardContent.Text);
            }
            catch (RegexMatchTimeoutException ex)
            {
                UserFeedback.ShowWarning($"RegexHandler '{HandlerConfig.Name}': Regex match timeout for input length {clipboardContent.Text.Length}");
                System.Diagnostics.Debug.WriteLine($"RegexHandler: Regex match timeout - {ex.Message}");
                return false;
            }
            catch (ArgumentException ex)
            {
                UserFeedback.ShowError($"RegexHandler '{HandlerConfig.Name}': Invalid input for regex matching - {ex.Message}");
                return false;
            }
        }

        async Task<bool> IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return await CanHandleAsync(clipboardContent);
        }

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            if (clipboardContent?.Text == null)
            {
                return new Dictionary<string, string>
                {
                    [ContextKey._error] = "Invalid clipboard content"
                };
            }

            string input = clipboardContent.Text;
            var context = new Dictionary<string, string>
            {
                [ContextKey._input] = input
            };

            try
            {
                var match = _compiledRegex.Match(input);

                if (match.Success)
                {
                    // Add full match
                    context[ContextKey._match] = match.Value;
                    
                    // Process named and indexed groups
                    if (HandlerConfig.Groups != null && HandlerConfig.Groups.Count > 0)
                    {
                        for (int i = 0; i < HandlerConfig.Groups.Count; i++)
                        {
                            var groupName = HandlerConfig.Groups[i];
                            string groupValue;

                            // Try to get named group first, then fall back to indexed group
                            var namedGroup = match.Groups[groupName];
                            if (namedGroup.Success)
                            {
                                groupValue = namedGroup.Value;
                            }
                            else
                            {
                                // Groups[0] is always the full match, actual capturing groups start from index 1
                                var groupIndex = i + 1;
                                groupValue = match.Groups.Count > groupIndex ? match.Groups[groupIndex].Value : string.Empty;
                            }

                            context[groupName] = groupValue;
                        }
                    }
                    else
                    {
                        // If no groups configured, add all captured groups with numeric keys
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            context[$"group_{i}"] = match.Groups[i].Value;
                        }
                    }
                }
                else
                {
                    context[ContextKey._error] = "Regex pattern did not match the input";
                    UserFeedback.ShowWarning($"RegexHandler '{HandlerConfig.Name}': Pattern did not match input");
                }
            }
            catch (RegexMatchTimeoutException ex)
            {
                context[ContextKey._error] = $"Regex match timeout: {ex.Message}";
                UserFeedback.ShowError($"RegexHandler '{HandlerConfig.Name}': {REGEX_MATCH_FAILED} - timeout");
                System.Diagnostics.Debug.WriteLine($"RegexHandler: Match timeout - {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                context[ContextKey._error] = $"Invalid regex operation: {ex.Message}";
                UserFeedback.ShowError($"RegexHandler '{HandlerConfig.Name}': {REGEX_MATCH_FAILED} - {ex.Message}");
            }

            return context;
        }

        protected override List<ConfigAction> GetActions()
        {
            return base.HandlerConfig.Actions;
        }
    }
}
