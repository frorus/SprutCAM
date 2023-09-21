using Microsoft.WindowsAPICodePack.Dialogs;
using SprutCAM.Commands;
using SprutCAM.Utils;
using SprutCAM.Utils.Model;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SprutCAM.ViewModel
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private RelayCommand _chooseFolderDialogCommand;
        private RelayCommand _downloadCommand;
        private RelayCommand _stopDownloadCommand;

        private string _filePath;
        private bool _isDownloadEnabled;
        private bool _isDownloadInProgress;
        private bool _isDownloadCompleted;
        private double _currentProgress;

        private CancellationTokenSource _cts;

        #region Properties
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }

        public bool IsDownloadEnabled
        {
            get { return _isDownloadEnabled; }
            set
            {
                _isDownloadEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsDownloadInProgress
        {
            get { return _isDownloadInProgress; }
            set
            {
                _isDownloadInProgress = value;
                OnPropertyChanged();
            }
        }

        public bool IsDownloadCompleted
        {
            get { return _isDownloadCompleted; }
            set
            {
                _isDownloadCompleted = value;
                OnPropertyChanged();
            }
        }

        public double CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                if (_currentProgress != value)
                {
                    _currentProgress = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region Relay commands
        public RelayCommand ChooseFolderDialogCommand
        {
            get
            {
                return _chooseFolderDialogCommand ??
                  (_chooseFolderDialogCommand = new RelayCommand(x =>
                  {
                      var dialog = new CommonOpenFileDialog
                      {
                          IsFolderPicker = true
                      };

                      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                      {
                          FilePath = dialog.FileName;
                          IsDownloadEnabled = true;
                      }
                  }));
            }
        }

        public RelayCommand DownloadCommand
        {
            get
            {
                return _downloadCommand ??
                  (_downloadCommand = new RelayCommand(async x =>
                  {

                      var downloadFileUrl = "https://download.sprutcam.com/links/SprutCAM_X.zip";
                      var destinationFilePath = Path.Combine(FilePath, "SprutCAM_X.zip");

                      _cts = new CancellationTokenSource();
                      CurrentProgress = 0;
                      IsDownloadInProgress = true;
                      IsDownloadCompleted = false;

                      JsonFileUtils.Write(
                          new InstallData { TimeStamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") },
                          Path.Combine(FilePath, "installdata.json"));

                      using (var client = new HttpClientDownloader(downloadFileUrl, destinationFilePath, _cts.Token))
                      {
                          client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
                          {
                              CurrentProgress = (double)progressPercentage;
                          };

                          await client.StartDownload();
                      }

                      if(CurrentProgress == 100)
                      {
                          IsDownloadInProgress = false;
                          IsDownloadCompleted = true;
                      }
                  }));
            }
        }

        public RelayCommand StopDownloadCommand
        {
            get
            {
                return _stopDownloadCommand ??
                  (_stopDownloadCommand = new RelayCommand(x =>
                  {
                      _cts.Cancel();
                      _cts.Dispose();
                      CurrentProgress = 0;
                      IsDownloadInProgress = false;
                  }));
            }
        }
        #endregion

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
