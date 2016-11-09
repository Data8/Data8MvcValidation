using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data8.MvcValidation
{
    static class ExtensionMethods
    {
        /// <summary>
        /// Converts a string to proper (title) case, optionally handling a few special cases for common surname patterns.
        /// </summary>
        /// <param name="str">The string to apply proper casing to.</param>
        /// <param name="lastname"><c>true</c> if the <paramref name="str"/> is to be interpreted as a surname, or <c>false</c> otherwise.</param>
        /// <param name="culture">The culture to perform the case transformations in.</param>
        /// <returns></returns>
        public static string ToProper(this string str, bool lastname, CultureInfo culture)
        {
            // Use the .NET Framework provided ToTitleCase method as a starting point.
            str = culture.TextInfo.ToTitleCase(str.ToLower(culture));

            if (lastname)
            {
                // Apply additional rules for surnames with a few common patterns that require an additional
                // capital letter in the middle of the word.
                var prefixes = new[] {"Mc", "Mac", "O'"};

                foreach (var prefix in prefixes)
                {
                    if (str.StartsWith(prefix, false, culture) && str.Length > prefix.Length + 1)
                    {
                        str = prefix + str.Substring(prefix.Length, 1).ToUpper() + str.Substring(prefix.Length + 1);
                        break;
                    }
                }
            }

            return str;
        }
    }
}
