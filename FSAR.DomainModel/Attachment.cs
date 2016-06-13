namespace FSAR.DomainModel
{
    public class Attachment : Entity
    {
        public string DisplayFileName { get; set; }

        public string FilePath { get; set; }

        public string OldFilePath { get; set; }

        public string RelocationErrorMessage { get; set; }
    }
}
