namespace DBBackup.Helpers
{
    public static class PathValidator
    {
        public static bool PathDirExists(string path)
        {
            string? directory = Path.GetDirectoryName(path);
            return Path.Exists(directory);
        }
    }
}
