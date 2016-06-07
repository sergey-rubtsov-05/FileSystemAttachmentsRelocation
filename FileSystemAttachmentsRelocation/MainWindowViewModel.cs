using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
        private CancellationTokenSource _cancellationToken;
        private readonly Dispatcher _dispatcher;
        private bool _canDoingAsyncCommand = true;

        private ICommand _getNotInCurrentDir;
        private bool _isDoingAsyncCommand;
        private long _ramUsage;
        private string _textOnProgressBar;
        private int _totalAttachmentsToRelocationCount;
        private int _attachmentsToRelocationCount;
        private string _driveName;
        private long _driveFreeSpace;
        private string _currentAttachmentsFolder;

        public MainWindowViewModel()
        {
            AttachmentsToRelocationCount = 0;
            InfoMessages = new ObservableCollection<string>();
            AttachementsWithErrors = new ObservableCollection<Guid>();
            CurrentAttachmentsFolder = @"C:\Uploads\TTK";
            _dispatcher = Dispatcher.CurrentDispatcher;
            _cancellationToken = new CancellationTokenSource();

            Task.Run(() =>
            {
                var doThis = true;
                var process = Process.GetCurrentProcess();
                while (doThis)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        process.Refresh();
                        var performanceCounter = new PerformanceCounter("Process", "Working Set - Private", process.ProcessName);
                        var ramUsage = performanceCounter.RawValue / 1024;
                        _dispatcher.Invoke(() => { RamUsage = ramUsage; });
                    }
                    catch (TaskCanceledException)
                    {
                        doThis = false;
                    }
                }
            });

            Task.Run(() =>
            {
                var doThis = true;
                while (doThis)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        if (string.IsNullOrWhiteSpace(CurrentAttachmentsFolder))
                            continue;

                        var drive = new DriveInfo(Path.GetPathRoot(CurrentAttachmentsFolder));
                        if (!drive.IsReady)
                        {
                            DriveFreeSpace = 0;
                            continue;
                        }
                        var freeSpace = drive.AvailableFreeSpace/1024/1024;
                        DriveName = drive.Name;
                        DriveFreeSpace = freeSpace;
                    }
                    catch (TaskCanceledException)
                    {
                        doThis = false;
                    }
                    catch (Exception)
                    {
                    }
                }
            });
        }

        public string CurrentAttachmentsFolder
        {
            get { return _currentAttachmentsFolder; }
            set
            {
                _currentAttachmentsFolder = value;
                NotifyPropertyChanged(nameof(CurrentAttachmentsFolder));
            }
        }

        public bool CanDoingAsyncCommand
        {
            get { return _canDoingAsyncCommand; }
            set
            {
                _canDoingAsyncCommand = value;
                NotifyPropertyChanged(nameof(CanDoingAsyncCommand));
            }
        }

        public ICommand GetNotInCurrentDir
        {
            get
            {
                return _getNotInCurrentDir ?? (_getNotInCurrentDir = new CommandHandler(() =>
                {
                    Task.Run(() =>
                    {
                        IsDoingAsyncCommand = true;
                        CanDoingAsyncCommand = false;
                        using (var attachmentRepo = new AttachmentRepository())
                        {
                            GetTotalCountAttachmentsNotInCurrentFolder(attachmentRepo);
                            _dispatcher.Invoke(OnAsyncCommandEnd);
                        }
                    });
                }, CanDoingAsyncCommand));
            }
        }

        private int GetTotalCountAttachmentsNotInCurrentFolder(AttachmentRepository attachmentRepo)
        {
            try
            {
                Log("Getting total count", true);
                TotalAttachmentsToRelocationCount =
                    attachmentRepo.GetTotalCountAttachmentsNotInCurrentFolder(CurrentAttachmentsFolder);
                Log($"Total count: {TotalAttachmentsToRelocationCount}");
                return TotalAttachmentsToRelocationCount;
            }
            catch (Exception e)
            {
                var message = $"Error on getting total count. Message: {e.Message}";
                Log(message);
                Logger.Instance.Error(message, e);
                return 0;
            }
        }

        public ICommand StartProcess
        {
            get
            {
                return new CommandHandler(() =>
                {
                    if (IsDoingAsyncCommand)
                    {
                        Log("Cannot start. One process already doing");
                        return;
                    }
                    _cancellationToken = new CancellationTokenSource();
                    IsDoingAsyncCommand = true;
                    CanDoingAsyncCommand = false;
                    Task.Run(() => ProcessAttachments(_cancellationToken.Token), _cancellationToken.Token);
                }, CanDoingAsyncCommand);
            }
        }

        public ObservableCollection<string> InfoMessages { get; set; }

        public ICommand StopProcess
        {
            get
            {
                return new CommandHandler(() =>
                {
                    _cancellationToken.Cancel();
                });
            }
        }

        public bool IsDoingAsyncCommand
        {
            get { return _isDoingAsyncCommand; }
            set
            {
                _isDoingAsyncCommand = value;
                NotifyPropertyChanged(nameof(IsDoingAsyncCommand));
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

        public int AttachmentsToRelocationCount
        {
            get { return _attachmentsToRelocationCount; }
            set
            {
                _attachmentsToRelocationCount = value;
                NotifyPropertyChanged(nameof(AttachmentsToRelocationCount));
            }
        }

        public ObservableCollection<Guid> AttachementsWithErrors{ get; set; }

        public string DriveName
        {
            get { return _driveName; }
            set
            {
                _driveName = value;
                NotifyPropertyChanged(nameof(DriveName));
            }
        }

        public long DriveFreeSpace
        {
            get { return _driveFreeSpace; }
            set
            {
                _driveFreeSpace = value;
                NotifyPropertyChanged(nameof(DriveFreeSpace));
            }
        }

        private void ProcessAttachments(CancellationToken token)
        {
            Log("Start");
            var doThis = true;
            while (doThis)
            {
                List<Attachment> attachments = new List<Attachment>();
                using (var attachmentRepo = new AttachmentRepository())
                {
                    var count = GetTotalCountAttachmentsNotInCurrentFolder(attachmentRepo);
                    if (count == 0)
                    {
                        doThis = false;
                    }
                    else if (count == AttachementsWithErrors.Count)
                    {
                        Log("Attachments only with errors");
                        doThis = false;
                    }
                    else if (count > 0)
                    {
                        attachments = attachmentRepo.GetAttachmentsNotInCurrentFolder(CurrentAttachmentsFolder);
                        AttachmentsToRelocationCount = attachments.Count;
                    }
                    else
                    {
                        doThis = false;
                    }
                }
                foreach (var attachment in attachments)
                {
                    if (token.IsCancellationRequested)
                    {
                        doThis = false;
                        break;
                    }
                    ProcessAttachment(attachment);
                    AttachmentsToRelocationCount -= 1;
                }
            }
            _dispatcher.Invoke(OnAsyncCommandEnd);
            Log(token.IsCancellationRequested ? "Process cancelled!" : "Done!");
        }

        private void OnAsyncCommandEnd()
        {
            IsDoingAsyncCommand = false;
            CanDoingAsyncCommand = true;
            TextOnProgressBar = string.Empty;
        }

        private void ProcessAttachment(Attachment attachment)
        {
            Log($"Begin process attachment id: {attachment.Id}, fileName: {Path.GetFileName(attachment.FilePath)}");
            try
            {
                var fsr = new FileSystemEngine();
                Log($"Old attachment file path: {attachment.FilePath}");
                var newActualPath = fsr.GetActualPath(attachment.FilePath, CurrentAttachmentsFolder);
                Log($"New attachment file path: {newActualPath}");
                Log("Coping and merging", true);
                var result = fsr.CopyFile(attachment.FilePath, newActualPath);
                if (result)
                {
                    attachment.FilePath = newActualPath;
                    using (var attachmentRepo = new AttachmentRepository())
                    {
                        Log("Attachment entity updating", true);
                        attachmentRepo.Update(attachment);
                    }
                }
                else
                {
                    Log($"Coping or merging for attachment {{{attachment.Id}}} was false");
                }
                Log($"End   process attachment id: {attachment.Id}, fileName: {Path.GetFileName(attachment.FilePath)}");
            }
            catch (Exception e)
            {
                var message = $"Error process attachment id: {attachment.Id}";
                Log($"{message}, error: {e.Message}");
                Logger.Instance.Error(message, e);
                if (!AttachementsWithErrors.Contains(attachment.Id))
                    AttachementsWithErrors.Add(attachment.Id);
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
    }
}