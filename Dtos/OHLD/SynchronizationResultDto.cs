namespace Dtos.OHLD
{
    public class SynchronizationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalRecordsProcessed { get; set; }
        public int NewRecordsAdded { get; set; }
        public int ExistingRecordsUpdated { get; set; }
        public int RecordsDeleted { get; set; }
        public DateTime SynchronizedAt { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}