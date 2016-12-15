﻿namespace Unosquare.Swan
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    partial class Extensions
    {
        static private readonly Lazy<MD5> MD5Hasher = new Lazy<MD5>(MD5.Create, true);
        static private readonly Lazy<SHA1> SHA1Hasher = new Lazy<SHA1>(SHA1.Create, true);
        static private readonly Lazy<SHA256> SHA256Hasher = new Lazy<SHA256>(SHA256.Create, true);
        static private readonly Lazy<SHA512> SHA512Hasher = new Lazy<SHA512>(SHA512.Create, true);

        static private readonly Lazy<Regex> UnderscoreRegex = new Lazy<Regex>(() =>
            { return new Regex(@"_", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.CultureInvariant); });

        static private readonly Lazy<Regex> CamelCaseRegEx = new Lazy<Regex>(() =>
            { return new Regex(@"[a-z][A-Z]", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.CultureInvariant); });

        static private readonly Lazy<MatchEvaluator> SplitCamelCaseString = new Lazy<MatchEvaluator>(() => {
            return new MatchEvaluator((m) =>
            {
                var x = m.ToString();
                return x[0] + " " + x.Substring(1, x.Length - 1);
            });
        });

        /// <summary>
        /// Computes the MD5 hash of the given string using UTF8 byte encoding.
        /// </summary>
        /// <param name="inputString">The input string.</param>
        /// <returns></returns>
        public static byte[] ComputeMD5(this string inputString)
        {
            var inputBytes = Encoding.UTF8.GetBytes(inputString);
            return MD5Hasher.Value.ComputeHash(inputBytes);
        }

        /// <summary>
        /// Computes the SHA-1 hash of the given string using UTF8 byte encoding.
        /// </summary>
        /// <param name="inputString">The input string.</param>
        /// <returns></returns>
        public static byte[] ComputeSha1(this string inputString)
        {
            var inputBytes = Encoding.UTF8.GetBytes(inputString);
            return SHA1Hasher.Value.ComputeHash(inputBytes);
        }

        /// <summary>
        /// Computes the SHA-256 hash of the given string using UTF8 byte encoding.
        /// </summary>
        /// <param name="inputString">The input string.</param>
        /// <returns></returns>
        public static byte[] ComputeSha256(this string inputString)
        {
            var inputBytes = Encoding.UTF8.GetBytes(inputString);
            return SHA256Hasher.Value.ComputeHash(inputBytes);
        }

        /// <summary>
        /// Computes the SHA-512 hash of the given string using UTF8 byte encoding.
        /// </summary>
        /// <param name="inputString">The input string.</param>
        /// <returns></returns>
        public static byte[] ComputeSha512(this string inputString)
        {
            var inputBytes = Encoding.UTF8.GetBytes(inputString);
            return SHA512Hasher.Value.ComputeHash(inputBytes);
        }

        /// <summary>
        /// Returns a string that represents the given item
        /// It tries to use InvariantCulture if the ToString(IFormatProvider)
        /// overload exists.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static string ToStringInvariant(this object item)
        {
            if (item == null)
                return string.Empty;

            var itemType = item.GetType();

            if (itemType == typeof(string))
                return item as string;

            return Constants.BasicTypesInfo.ContainsKey(itemType) ?
                Constants.BasicTypesInfo[itemType].ToStringInvariant(item) :
                item.ToString();
        }

        /// <summary>
        /// Returns a string that represents the given item
        /// It tries to use InvariantCulture if the ToString(IFormatProvider)
        /// overload exists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static string ToStringInvariant<T>(this T item)
        {
            if (typeof(string) == typeof(T))
                return item == null ? string.Empty : item as string;

            return ToStringInvariant(item as object);
        }

        /// <summary>
        /// Humanizes (make more human-readable) an identifier-style string 
        /// in either camel case or snake case. For example, CamelCase will be converted to 
        /// Camel Case and Snake_Case will be converted to Snake Case.
        /// </summary>
        /// <param name="identifierString">The identifier-style string.</param>
        /// <returns></returns>
        static public string Humanize(this string identifierString)
        {
            var returnValue = identifierString ?? string.Empty;
            returnValue = UnderscoreRegex.Value.Replace(returnValue, " ");
            returnValue = CamelCaseRegEx.Value.Replace(returnValue, SplitCamelCaseString.Value);
            return returnValue;
        }


        // TODO: Test Humanize, Add pluralize, singularize, dashize, capitalize titleize, etc.
    }
}
