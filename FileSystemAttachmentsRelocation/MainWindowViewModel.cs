using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
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
            _dispatcher = Dispatcher.CurrentDispatcher;
            _cancellationToken = new CancellationTokenSource();
        }

        public ObservableCollection<Attachment> AttachmentsToRelocation { get; set; }

        public string CurrentAttachmentsFolder { get; set; }

        private ICommand _getNotInCurrentDir;
        private Dispatcher _dispatcher;
        private readonly CancellationTokenSource _cancellationToken;

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
                    var attachments = AttachmentsToRelocation.ToList();
                    Task.Run(() => ProcessAttachments(attachments, _cancellationToken.Token), _cancellationToken.Token);
                });
            }
        }

        private void ProcessAttachments(IList<Attachment> attachmentsToRelocation, CancellationToken token)
        {
            Log("Start");
            foreach (var attachment in attachmentsToRelocation)
            {
                ProcessAttachment(attachment);
                if (token.IsCancellationRequested)
                {
                    Log("Process cancelled!");
                    break;
                }
            }
            Log("Done!");
        }

        private void ProcessAttachment(Attachment attachment)
        {
            Log($"Begin process attachment id: {attachment.Id}, fileName: {Path.GetFileName(attachment.FilePath)}");
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
                Log($"End   process attachment id: {attachment.Id}, fileName: {Path.GetFileName(attachment.FilePath)}");
            }
            catch (Exception e)
            {
                Log($"Error process attachment id: {attachment.Id}, error: {e.Message}");
                Log(e.StackTrace);
            }
        }

        private void Log(string message)
        {
            _dispatcher.Invoke(() => { InfoMessages.Add($"{DateTime.Now.ToString("O")} - {message}"); });
        }

        public ObservableCollection<string> InfoMessages { get; set; }

        public ICommand StopProcess
        {
            get
            {
                return new CommandHandler(() =>
                {
                    _cancellationToken.Cancel();
                    AttachmentsToRelocation.Clear();
                });
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