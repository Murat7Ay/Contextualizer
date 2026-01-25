using System;
using System.Security.Cryptography;
using System.Text;

namespace Contextualizer.Core.FunctionProcessing.FunctionExecutors
{
    internal static class HashFunctionExecutor
    {
        public static string ProcessHashFunction(string functionName, string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Hash functions require 1 parameter: text to hash");

            var text = parameters[0];
            var hashType = functionName.Substring(5).ToLower(); // Remove "hash." prefix

            return hashType switch
            {
                "md5" => ComputeMD5Hash(text),
                "sha256" => ComputeSHA256Hash(text),
                _ => throw new NotSupportedException($"Hash type '{hashType}' is not supported")
            };
        }

        private static string ComputeMD5Hash(string input)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = md5.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }

        private static string ComputeSHA256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }
    }
}
