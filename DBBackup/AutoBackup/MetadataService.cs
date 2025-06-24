using System.Text.Json;

namespace DBBackup.AutoBackup
{
    public static class MetadataService
    {
        private const string _metaDir = ".metadata";

        public static void CreateMetaDir()
        {
            if (!Directory.Exists(_metaDir))
            {
                Directory.CreateDirectory(_metaDir);
            }
        }

        public static void WriteMetadata(FileMetadata metadata)
        {
            string metaPath = MetaPath(metadata.FilePath);
            string jsonMeta = JsonSerializer.Serialize(metadata);
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
            File.WriteAllText(metaPath, jsonMeta);
        }

        public static IEnumerable<FileMetadata> GetMetadata()
        {
            DirectoryInfo metaDirectory = new DirectoryInfo(_metaDir);
            FileInfo[] files = metaDirectory.GetFiles();
            return files.Select(file => JsonSerializer.Deserialize<FileMetadata>(File.ReadAllText(file.FullName))!);
        }

        public static void DeleteMetadata(string forFilePath)
        {
            string metaPath = MetaPath(forFilePath);
            if (File.Exists(metaPath)) File.Delete(metaPath);
        }

        private static string MetaPath(string path)
        {
            string filename = Path.GetFileName(path);
            string metaPath = Path.Combine(_metaDir, $"{filename}.metadata");
            return metaPath;
        }
    }
}
