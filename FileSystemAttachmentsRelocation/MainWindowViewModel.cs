using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using FSAR.DataAccessLayer;
using FSAR.DomainModel;
using FSAR.Engine;

namespace FileSystemAttachmentsRelocation
{
    public class MainWindowViewModel
    {
        public MainWindowViewModel()
        {
            AttachmentsToRelocation = new ObservableCollection<Attachment>();
            InfoMessages = new ObservableCollection<string>();
            CurrentAttachmentsFolder = @"C:\Uploads\TTK";
        }

        public ObservableCollection<Attachment> AttachmentsToRelocation { get; set; }

        public string CurrentAttachmentsFolder { get; set; }

        private ICommand _getNotInCurrentDir;
        public ICommand GetNotInCurrentDir
        {
            get
            {
                return _getNotInCurrentDir ?? (_getNotInCurrentDir = new CommandHandler(() =>
                {
                    using (var attachmentEngine = new AttachmentRepository())
                    {
                        var attachments = attachmentEngine.GetAttachmentsNotInCurrentFolder(CurrentAttachmentsFolder);
                        foreach (var attachment in attachments)
                        {
                            AttachmentsToRelocation.Add(attachment);
                        }
                    }
                }));
            }
        }

        public ICommand StartProcess
        {
            get
            {
                return new CommandHandler(() =>
                {
                    if (AttachmentsToRelocation == null || !AttachmentsToRelocation.Any())
                    {
                        Log("Attachment collection is empty or null");
                        return;
                    }

                    foreach (var attachment in AttachmentsToRelocation)
                    {
                        ProcessAttachment(attachment);
                    }

                    Log("Done!");
                });
            }
        }

        private void ProcessAttachment(Attachment attachment)
        {
            Log($"Process attachment id: {attachment.Id}, fileName: {Path.GetFileName(attachment.FilePath)}");
            try
            {
                var fsr = new DummyEngine();
                var newActualPath = fsr.GetActualPath(attachment.FilePath, CurrentAttachmentsFolder);
                fsr.CopyFile(attachment.FilePath, newActualPath);
                if (fsr.MergeMd5FileHash(attachment.FilePath, newActualPath))
                {
                    attachment.FilePath = newActualPath;
                    using (var attachmentRepo = new AttachmentRepository())
                    {
                        attachmentRepo.Update(attachment);
                    }
                }
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }

        private void Log(string message)
        {
            InfoMessages.Add($"{DateTime.Now.ToString("O")} - {message}");
        }

        public ObservableCollection<string> InfoMessages { get; set; }
    }

    public class CommandHandler : ICommand
    {
        private readonly Action _action;
        public CommandHandler(Action action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}