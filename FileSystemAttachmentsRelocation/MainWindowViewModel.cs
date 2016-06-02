using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using FSAR.DataAccessLayer;
using FSAR.DomainModel;
using FSAR.Engine;
using FSAR.Logger;

namespace FileSystemAttachmentsRelocation
{
    public class MainWindowViewModel : Notifier
    {
        public MainWindowViewModel()
        {
            AttachmentsToRelocation = new ObservableCollection<Attachment>();
            InfoMessages = new ObservableCollection<string>();
            CurrentAttachmentsFolder = @"C:\Uploads\TTK";
            _dispatcher = Dispatcher.CurrentDispatcher;
            _cancellationToken = new CancellationTokenSource();

            Task.Run(() =>
            {
                bool doThis = true;
                var process = Process.GetCurrentProcess();
                while (doThis)
                {
                    Thread.Sleep(500);
                    try
                    {
                        process.Refresh();
                        var ramUsage = process.PrivateMemorySize64/1024;
                        _dispatcher.Invoke(() => { RamUsage = ramUsage; });
                    }
                    catch (TaskCanceledException)
                    {
                        doThis = false;
                    }
                }
            });
        }

        public ObservableCollection<Attachment> AttachmentsToRelocation { get; set; }

        public string CurrentAttachmentsFolder { get; set; }

        private ICommand _getNotInCurrentDir;
        private readonly Dispatcher _dispatcher;
        private readonly CancellationTokenSource _cancellationToken;
        private bool _isProcessDoing;
        private string _textOnProgressBar;
        private int _totalAttachmentsToRelocationCount;
        private long _ramUsage;
        private bool _canStartProcess = true;

        public bool CanStartProcess
        {
            get { return _canStartProcess; }
            set
            {
                _canStartProcess = value;
                NotifyPropertyChanged(nameof(CanStartProcess));
            }
        }

        public ICommand GetNotInCurrentDir
        {
            get
            {
                return _getNotInCurrentDir ?? (_getNotInCurrentDir = new CommandHandler(() =>
                {
                    using (var attachmentRepo = new AttachmentRepository())
                    {
                        TotalAttachmentsToRelocationCount =
                            attachmentRepo.GetTotalCountAttachmentsNotInCurrentFolder(CurrentAttachmentsFolder);
                        var attachments = attachmentRepo.GetAttachmentsNotInCurrentFolder(CurrentAttachmentsFolder);
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
                    if (IsProcessDoing)
                    {
                        Log("Cannot start. One process alredy doing");
                        return;
                    }
                    var attachments = AttachmentsToRelocation.ToList();
                    IsProcessDoing = true;
                    CanStartProcess = false;
                    Task.Run(() => ProcessAttachments(attachments, _cancellationToken.Token), _cancellationToken.Token);
                }, CanStartProcess);
            }
        }

        private void ProcessAttachments(IList<Attachment> attachmentsToRelocation, CancellationToken token)
        {
            Log("Start");
            foreach (var attachment in attachmentsToRelocation)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }
                ProcessAttachment(attachment);
            }
            _dispatcher.Invoke(() =>
            {
                AttachmentsToRelocation.Clear();
                IsProcessDoing = false;
                CanStartProcess = true;
                TextOnProgressBar = string.Empty;
            });
            Log(token.IsCancellationRequested ? "Process cancelled!" : "Done!");
        }

        private void ProcessAttachment(Attachment attachment)
        {
            Log($"Begin process attachment id: {attachment.Id}, fileName: {Path.GetFileName(attachment.FilePath)}");
            try
            {
                var fsr = new DummyEngine();
                var newActualPath = fsr.GetActualPath(attachment.FilePath, CurrentAttachmentsFolder);
                Log("Coping", true);
                fsr.CopyFile(attachment.FilePath, newActualPath);
                Log("Merging", true);
                if (fsr.MergeMd5FileHash(attachment.FilePath, newActualPath))
                {
                    attachment.FilePath = newActualPath;
                    using (var attachmentRepo = new AttachmentRepository())
                    {
                        Log("Attachment entity updating", true);
                        attachmentRepo.Update(attachment);
                    }
                }
                Log($"End   process attachment id: {attachment.Id}, fileName: {Path.GetFileName(attachment.FilePath)}");
            }
            catch (Exception e)
            {
                var message = $"Error process attachment id: { attachment.Id}";
                Log($"{message}, error: {e.Message}");
                Logger.Instance.Error(message, e);
            }
        }

        private void Log(string message, bool onProgressBar = false)
        {
            _dispatcher.Invoke(() =>
            {
                InfoMessages.Add($"{DateTime.Now.ToString("O")} - {message}");
                if (onProgressBar)
                    TextOnProgressBar = message;
                Logger.Instance.Info(message);
            });
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

        public bool IsProcessDoing
        {
            get { return _isProcessDoing; }
            set
            {
                _isProcessDoing = value;
                NotifyPropertyChanged(nameof(IsProcessDoing));
            }
        }

        public string TextOnProgressBar
        {
            get { return _textOnProgressBar; }
            set
            {
                _textOnProgressBar = value;
                NotifyPropertyChanged(nameof(TextOnProgressBar));
            }
        }

        public int TotalAttachmentsToRelocationCount
        {
            get { return _totalAttachmentsToRelocationCount; }
            set
            {
                _totalAttachmentsToRelocationCount = value;
                NotifyPropertyChanged(nameof(TotalAttachmentsToRelocationCount));
            }
        }

        public long RamUsage
        {
            get { return _ramUsage; }
            set
            {
                _ramUsage = value;
                NotifyPropertyChanged(nameof(RamUsage));
            }
        }
    }
}