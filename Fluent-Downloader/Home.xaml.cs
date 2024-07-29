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
            PathInfoBar.IsOpen = false;


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

            PathInfoBar.IsOpen = false;


            // 入力値を取得
            string url = URLTextBox.Text;
            string outputFolderPath = OutputFolderPathTextBox.Text;

            // URLが空の場合、エラーメッセージを表示
            if (string.IsNullOrEmpty(url))
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "The URL field is empty.",
                    PrimaryButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                };
                await errorDialog.ShowAsync();
                return;
            }

            // 出力フォルダパスが空の場合、エラーメッセージを表示
            if (string.IsNullOrEmpty(outputFolderPath))
            {
                var successDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "The output folder path is empty.",
                    PrimaryButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                };
                await successDialog.ShowAsync();
                return;
            }




            try
            {

                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe");
                PathInfoBar.IsOpen = true;
                PathInfoBar.Message = ffmpegPath;


                var _youtubeClient = new YoutubeClient();

                // 動画情報を取得
                var video = await _youtubeClient.Videos.GetAsync(url);

                // 動画のストリーム情報を取得
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(video.Id);

                // 最高画質のビデオストリームを選択
                var videoStreamInfo = streamManifest.GetVideoStreams().OrderByDescending(s => s.Bitrate).First();

                // 最高画質のオーディオストリームを選択
                var audioStreamInfo = streamManifest.GetAudioStreams().OrderByDescending(s => s.Bitrate).First();

                // ファイル名を作成
                string fileName = Path.Combine(outputFolderPath, video.Title + ".mp4");

                // ダウンロード処理
                double totalProgress = 0;
                double currentProgress = 0;
                var progress = new Progress<double>(percentage =>
                {
                    // 進捗状況を更新
                    currentProgress = percentage;
                    totalProgress = (currentProgress / 2) + (currentProgress / 2);
                    DownloadProgressBar.Value = totalProgress;
                });

                // ビデオストリームをダウンロード
                var videoFileName = Path.Combine(outputFolderPath, video.Title + "-video.mp4");
                await _youtubeClient.Videos.Streams.DownloadAsync(
                    videoStreamInfo,
                    videoFileName,
                    progress
                );

                // オーディオストリームをダウンロード
                var audioFileName = Path.Combine(outputFolderPath, video.Title + "-audio.m4a");
                await _youtubeClient.Videos.Streams.DownloadAsync(
                    audioStreamInfo,
                    audioFileName,
                    progress
                );

                // オーディオストリームとビデオストリームを結合
                using (var videoFile = new FileStream(videoFileName, FileMode.Open, FileAccess.Read))
                using (var audioFile = new FileStream(audioFileName, FileMode.Open, FileAccess.Read))
                {
                    // FFmpeg を使用してオーディオとビデオを結合
                    // FFmpeg は別途インストールする必要があります
                    // FFmpeg のパスを適宜調整してください





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

                // 音声ファイル削除
                File.Delete(audioFileName);
                File.Delete(videoFileName);

                var successDialog = new ContentDialog
                {
                    Title = "Success",
                    Content = "Download completed successfully.",
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
                    Title = "Error",
                    Content = "An error occurred during the download.",
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
