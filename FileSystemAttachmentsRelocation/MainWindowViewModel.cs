using System.Collections.ObjectModel;
using FSAR.DataAccessLayer;
using FSAR.DomainModel;

namespace FileSystemAttachmentsRelocation
{
    public class MainWindowViewModel
    {
        public MainWindowViewModel()
        {
            AttachmentsToRelocation = new ObservableCollection<Attachment>();

            using (var attachmentEngine = new AttachmentEngine())
            {
                var attachments = attachmentEngine.GetAttachmentByFilePathMask(string.Empty);
                foreach (var attachment in attachments)
                {
                    AttachmentsToRelocation.Add(attachment);
                }
            }
        }

        public ObservableCollection<Attachment> AttachmentsToRelocation { get; set; }
    }
}