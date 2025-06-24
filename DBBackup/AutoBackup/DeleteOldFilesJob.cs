using Quartz;

namespace DBBackup.AutoBackup
{
    public class DeleteOldFilesJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            IEnumerable<FileMetadata> filesMetadata = MetadataService.GetMetadata();
            foreach (FileMetadata fileMetadata in filesMetadata)
            {
                if (DateTime.Now >= fileMetadata.DeleteOn)
                {
                    if (File.Exists(fileMetadata.FilePath)) File.Delete(fileMetadata.FilePath);
                    MetadataService.DeleteMetadata(fileMetadata.FilePath);
                }
            }
            return Task.CompletedTask;
        }
    }
}
