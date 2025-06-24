namespace DBBackup.AutoBackup
{
    public class FileMetadata
    {
        public string FilePath {  get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime DeleteOn {  get; set; }
        public FileMetadata(string filePath, DateTime createdOn, DateTime deleteOn)
        {
            FilePath = filePath;
            CreatedOn = createdOn;
            DeleteOn = deleteOn;
        }
    }
}
