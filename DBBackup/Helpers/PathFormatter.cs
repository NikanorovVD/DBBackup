using System.Text.RegularExpressions;

namespace DBBackup.Helpers
{
    public static partial class PathFormatter
    {
        public static string ReplaceDateTimePlaceholders(string path, DateTime dateTime)
        {
            return DateTimePlaceholdersRegex().Replace(path, match => 
                 dateTime.ToString(match.Groups[1].Value)
            );
        }

        [GeneratedRegex(@"\{(.*?)\}")]
        private static partial Regex DateTimePlaceholdersRegex();
    }
}
