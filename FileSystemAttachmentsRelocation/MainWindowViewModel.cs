using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using FSAR.DataAccessLayer;
using FSAR.DomainModel;

namespace FileSystemAttachmentsRelocation
{
    public class MainWindowViewModel
    {
        public MainWindowViewModel()
        {
            AttachmentsToRelocation = new ObservableCollection<Attachment>();
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
                    using (var attachmentEngine = new AttachmentEngine())
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