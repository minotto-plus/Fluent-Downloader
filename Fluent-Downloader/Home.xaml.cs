using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using WinRT.Interop;
using YoutubeExplode;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Fluent_Downloader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        public Home()
        {
            this.InitializeComponent();


        }
        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();

            InitializeWithWindow.Initialize(picker,
                WindowNative.GetWindowHandle(MainWindow.Handle));

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null) OutputFolderPathTextBox.Text = folder.Path.ToString();
        }


        private YoutubeClient _youtubeClient = new YoutubeClient();



        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadProgressBar.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            DownloadProgressBar.ShowPaused = false;
            DownloadProgressBar.ShowError = false;

            // ���͒l���擾
            string url = URLTextBox.Text;
            string outputFolderPath = OutputFolderPathTextBox.Text;

            // URL����̏ꍇ�A�G���[���b�Z�[�W��\��
            if (string.IsNullOrEmpty(url))
            {
                var errorDialog = new ContentDialog
                {
                    Title = "�G���[",
                    Content = "URL����ł��B",
                    PrimaryButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                };
                await errorDialog.ShowAsync();
                return;
            }

            // �o�̓t�H���_�p�X����̏ꍇ�A�G���[���b�Z�[�W��\��
            if (string.IsNullOrEmpty(outputFolderPath))
            {
                var successDialog = new ContentDialog
                {
                    Title = "����",
                    Content = "�o�̓t�H���_�p�X����ł��B",
                    PrimaryButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                };
                await successDialog.ShowAsync();
                return;
            }




            try
            {
                var _youtubeClient = new YoutubeClient();

                // ��������擾
                var video = await _youtubeClient.Videos.GetAsync(url);

                // ����̃X�g���[�������擾
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);

                // �ō��掿�̃r�f�I�X�g���[����I��
                var videoStreamInfo = streamManifest.GetVideoStreams().OrderByDescending(s => s.Bitrate).First();

                // �ō��掿�̃I�[�f�B�I�X�g���[����I��
                var audioStreamInfo = streamManifest.GetAudioStreams().OrderByDescending(s => s.Bitrate).First();

                // �t�@�C�������쐬
                string fileName = Path.Combine(outputFolderPath, video.Title + ".mp4");

                // �_�E�����[�h����
                double totalProgress = 0;
                double currentProgress = 0;
                var progress = new Progress<double>(percentage =>
                {
                    // �i���󋵂��X�V
                    currentProgress = percentage;
                    totalProgress = (currentProgress / 2) + (currentProgress / 2);
                    DownloadProgressBar.Value = totalProgress;
                });

                // �r�f�I�X�g���[�����_�E�����[�h
                var videoFileName = Path.Combine(outputFolderPath, video.Title + "-video.mp4");
                await _youtubeClient.Videos.Streams.DownloadAsync(
                    videoStreamInfo,
                    videoFileName,
                    progress
                );

                // �I�[�f�B�I�X�g���[�����_�E�����[�h
                var audioFileName = Path.Combine(outputFolderPath, video.Title + "-audio.m4a");
                await _youtubeClient.Videos.Streams.DownloadAsync(
                    audioStreamInfo,
                    audioFileName,
                    progress
                );

                // �I�[�f�B�I�X�g���[���ƃr�f�I�X�g���[��������
                using (var videoFile = new FileStream(videoFileName, FileMode.Open, FileAccess.Read))
                using (var audioFile = new FileStream(audioFileName, FileMode.Open, FileAccess.Read))
                {
                    // FFmpeg ���g�p���ăI�[�f�B�I�ƃr�f�I������
                    // FFmpeg �͕ʓr�C���X�g�[������K�v������܂�
                    // FFmpeg �̃p�X��K�X�������Ă�������


                    string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe"); ;





                    var pathDialog = new ContentDialog
                    {
                        Title = "����",
                        Content = ffmpegPath,
                        PrimaryButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot,
                    };
                    await pathDialog.ShowAsync();


                    var ffmpegProcess = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = ffmpegPath,
                            Arguments = $"-i \"{videoFileName}\" -i \"{audioFileName}\" -c copy -map 0:v -map 1:a \"{fileName}\" -y",
                            CreateNoWindow = false,
                            UseShellExecute = true
                        }
                    };
                    ffmpegProcess.Start();
                    ffmpegProcess.WaitForExit();
                }

                // �����t�@�C���폜
                File.Delete(audioFileName);
                File.Delete(videoFileName);

                var successDialog = new ContentDialog
                {
                    Title = "����",
                    Content = "�_�E�����[�h���������܂����B",
                    PrimaryButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                };
                await successDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                DownloadProgressBar.ShowPaused = false;
                DownloadProgressBar.ShowError = true;
                var errorDialog = new ContentDialog
                {
                    Title = "�G���[",
                    Content = "�_�E�����[�h���ɃG���[���������܂����B",
                    PrimaryButtonText = "Not OK",
                    XamlRoot = this.Content.XamlRoot,
                };
                await errorDialog.ShowAsync();
            }
            DownloadProgressBar.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            _youtubeClient = null;

        }
    }
}
