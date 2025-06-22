namespace DBBackup.Helpers
{
    public static class FileSizeString
    {
        public static string Get(long bytes)
        {
            string[] sizes = ["b", "Kb", "Mb", "Gb", "Tb"];
            int order = 0;
            while (bytes >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                bytes /= 1024;
            }
            return $"{bytes} {sizes[order]}";
        }
    }
}
