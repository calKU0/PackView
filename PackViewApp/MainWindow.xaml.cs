using LibVLCSharp.Shared;
using Microsoft.Win32;
using PackViewApp.Helpers;
using PackViewApp.Models;
using PackViewApp.Services;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using Path = System.IO.Path;

namespace PackViewApp
{
    public partial class MainWindow : Window
    {
        #region Configuration Parameters

        private readonly string _username = ConfigurationManager.AppSettings["HikvisionUsername"]!;
        private readonly string _password = ConfigurationManager.AppSettings["HikvisionPassword"]!;
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnectionString"].ConnectionString!;

        #endregion Configuration Parameters

        private readonly List<string> _productIds;
        private readonly long _documentId;

        private readonly DatabaseService _databaseService;
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        private readonly List<ProductInfo> _products = new();
        private ProductInfo _currentProduct;

        private bool _isFading = false;
        private string _lastMessage = "";
        private bool _isDraggingSlider = false;
        private TimeSpan _totalVideoDuration = TimeSpan.Zero;
        private DateTime _videoStartTime;

        public MainWindow(List<string> productIds, long documentId)
        {
            InitializeComponent();
            Core.Initialize();

            // Parameters
            _productIds = productIds;
            _documentId = documentId;

            // Inicializations
            _databaseService = new DatabaseService(_connectionString);
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);

            _mediaPlayer.Playing += (s, e) =>
            {
                Dispatcher.InvokeAsync(async () =>
                {
                    loadingText.Visibility = Visibility.Collapsed;
                    ControlsPanel.Visibility = Visibility.Visible;
                    StartButton.Visibility = Visibility.Collapsed;
                    StopButton.Visibility = Visibility.Visible;
                    BeginningButton.Visibility = Visibility.Collapsed;

                    await Task.Delay(4500);

                    _timer.Start();
                });
            };

            _mediaPlayer.EncounteredError += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    loadingText.Text = "Błąd podczas odtwarzania RTSP.";
                    MessageBox.Show("Błąd podczas odtwarzania RTSP.");
                });
            };

            _mediaPlayer.EndReached += async (s, e) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    SaveButton.IsEnabled = true;
                    BeginningButton.Visibility = Visibility.Visible;

                    if (_currentProduct != null)
                    {
                        _currentProduct.IsRecordingComplete = true;
                        var result = MessageBox.Show("Zakończono nagranie. Czy zapisać plik?", "Pytanie", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            VideoHelpers.CopyFile(_currentProduct.TempFilePath, _currentProduct.ProductCode);
                        }
                    }
                });
            };

            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLoadingStatus("Pobieranie danych...");

            foreach (var productId in _productIds)
            {
                _products.Add(await _databaseService.GetProductInfo(productId, _documentId));
            }

            FillProductsWithData(_products);

            videoView.MediaPlayer = _mediaPlayer;
            ProductListBox.ItemsSource = _products;

            ProductListBox.SelectedIndex = 0;
        }

        private void FillProductsWithData(List<ProductInfo> products)
        {
            if (products == null || !products.Any())
                return;

            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];

                if (product.ScanDate == null)
                    continue;

                // Calculate start time
                DateTime scanDate = product.ScanDate.Value.AddHours(-4).AddSeconds(-5);
                product.StartTime = scanDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                string endDateStr = string.Empty;

                if (products.Count == 1)
                {
                    if (product.SentDate.HasValue)
                    {
                        DateTime endDate = product.SentDate.Value.AddHours(-4).AddSeconds(30);
                        product.EndTime = endDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    }
                }
                else if (i < products.Count - 1)
                {
                    // End time is scan time of the next product
                    var nextProduct = products[i + 1];
                    if (nextProduct.ScanDate.HasValue)
                    {
                        DateTime endDate = nextProduct.ScanDate.Value.AddHours(-4);
                        product.EndTime = endDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    }
                }
                else
                {
                    // Last product
                    if (product.SentDate.HasValue)
                    {
                        DateTime endDate = product.SentDate.Value.AddHours(-4).AddSeconds(30);
                        product.EndTime = endDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    }
                }
            }
        }

        private async Task PlayVideoAsync(ProductInfo product)
        {
            _currentProduct = product;
            UpdateLoadingStatus("Ładowanie...");
            try
            {
                // If we already downloaded the video, play it locally
                if (!string.IsNullOrEmpty(product.TempFilePath) && File.Exists(product.TempFilePath) && product.IsRecordingComplete)
                {
                    var media = new Media(_libVLC, product.TempFilePath, FromType.FromPath);
                    _mediaPlayer.Play(media);
                    ShowSeekSlider(true);
                    return;
                }

                ShowSeekSlider(false);

                // If not, download from RTSP and save
                string formattedStartTime = DateTime.Parse(product.StartTime).ToString("yyyyMMdd'T'HHmmss'Z'");
                string formattedEndTime = DateTime.Parse(product.EndTime).ToString("yyyyMMdd'T'HHmmss'Z'");

                var rtspUri = $"rtsp://{_username}:{_password}@{product.Camera}/Streaming/tracks/101/?starttime={formattedStartTime}&endtime={formattedEndTime}";

                product.TempFilePath = Path.Combine(Path.GetTempPath(), $"hik_record_{Guid.NewGuid()}.mp4");

                var mediaRtsp = new Media(_libVLC, rtspUri, FromType.FromLocation);
                mediaRtsp.AddOption($":sout=#duplicate{{dst=display,dst=standard{{access=file,mux=mp4,dst={product.TempFilePath}}}}}");
                mediaRtsp.AddOption(":sout-keep");
                mediaRtsp.AddOption(":network-caching=500");
                mediaRtsp.AddOption(":no-sout-all");

                _videoStartTime = DateTime.Parse(product.StartTime);
                _totalVideoDuration = DateTime.Parse(product.EndTime) - _videoStartTime;

                _mediaPlayer.Play(mediaRtsp);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas odtwarzania: " + ex.Message);
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Play();
            StartButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Visible;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Pause();
            StartButton.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Collapsed;
        }

        private void videoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            UpdateLoadingStatus("Błąd przy próbie załadowania wideo.");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();

            foreach (var video in _products)
            {
                if (!string.IsNullOrEmpty(video.TempFilePath) && File.Exists(video.TempFilePath))
                {
                    try
                    {
                        File.Delete(video.TempFilePath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private async void UpdateLoadingStatus(string message, bool fade = true)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(message) || message == _lastMessage)
                    return;

                _lastMessage = message;

                if (_isFading)
                    return;

                _isFading = true;

                if (!fade)
                {
                    loadingText.Text = message;
                    loadingText.Opacity = 1; // Ensure it's visible
                    _isFading = false;
                    return;
                }

                // Fade-out if currently visible
                if (loadingText.Opacity > 0)
                {
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
                    loadingText.BeginAnimation(OpacityProperty, fadeOut);
                    await Task.Delay(200);
                }

                // Update text and fade-in
                loadingText.Text = message;
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                loadingText.BeginAnimation(OpacityProperty, fadeIn);

                _isFading = false;
            });
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            VideoHelpers.CopyFile(_currentProduct.TempFilePath, _currentProduct.ProductCode);
        }

        private void BeginningButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentProduct.TempFilePath) || !File.Exists(_currentProduct.TempFilePath))
            {
                MessageBox.Show("Plik wideo nie został jeszcze zapisany.");
                return;
            }

            // Play media from downloaded local file
            var media = new Media(_libVLC, _currentProduct.TempFilePath, FromType.FromPath);
            _mediaPlayer.Media = media;

            _mediaPlayer.Play();

            StartButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Visible;
            BeginningButton.Visibility = Visibility.Collapsed;

            // Enable seek slider
            ShowSeekSlider(true);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_mediaPlayer.Media == null || _isDraggingSlider)
                return;

            var duration = _mediaPlayer.Length;
            var currentTime = _mediaPlayer.Time;
            TimeSpan timeElapsed;
            TimeSpan timeRemaining;

            if (duration > 0)
            {
                SeekSlider.Value = (double)currentTime / duration * 100;
                timeElapsed = TimeSpan.FromMilliseconds(currentTime);
                timeRemaining = TimeSpan.FromMilliseconds(duration - currentTime);
            }
            else
            {
                timeElapsed = TimeSpan.FromMilliseconds(currentTime);
                timeRemaining = _totalVideoDuration - timeElapsed;
            }

            if (timeRemaining < TimeSpan.Zero)
                timeRemaining = TimeSpan.Zero;

            RemainingTimeText.Text = $"Pozostały czas: {timeRemaining:mm\\:ss}";
        }

        private async void ProductListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductListBox.SelectedItem is ProductInfo product)
            {
                if (_currentProduct != null && _currentProduct == product)
                    return;

                // Updating UI
                ProductCodeText.Text = product.ProductCode;
                ProductNameText.Text = product.ProductName;
                QuantityText.Text = $"{product.QuantityPacked} / {product.QuantityToPack} {product.ProductUnit}";
                ScanDateText.Text = product.ScanDate?.ToString("yyyy-MM-dd HH:mm:ss");
                StationText.Text = product.Station;
                OperatorText.Text = product.Operator;

                if (product.ProductImage != null && product.ProductImage.Length > 0)
                {
                    try
                    {
                        ProductImage.Source = ImageHelpers.LoadImage(product.ProductImage, product.ImageSize);
                    }
                    catch (Exception ex)
                    {
                        ProductImage.Source = null;
                    }
                }
                else
                {
                    ProductImage.Source = null;
                }

                // If previous video temp file exists but recording is NOT complete, delete it
                if (_currentProduct != null && !string.IsNullOrEmpty(_currentProduct.TempFilePath) && File.Exists(_currentProduct.TempFilePath) && !_currentProduct.IsRecordingComplete)
                {
                    try
                    {
                        _mediaPlayer.Stop();
                        _timer.Stop();

                        File.Delete(_currentProduct.TempFilePath);
                        _currentProduct.TempFilePath = "";
                    }
                    catch
                    {
                        // ignore errors
                    }
                }
                else
                {
                    _mediaPlayer.Stop();
                    _timer.Stop();
                }

                await PlayVideoAsync(product);
            }
        }

        private void SeekSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = true;
        }

        private void SeekSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_mediaPlayer.Media != null)
            {
                var percentage = SeekSlider.Value / 100;
                var newTime = (long)(_mediaPlayer.Length * percentage);
                _mediaPlayer.Time = newTime;
            }

            _isDraggingSlider = false;
        }

        private void ShowSeekSlider(bool show)
        {
            if (show)
            {
                SeekSlider.Visibility = Visibility.Visible;
                SeekSliderRow.Height = GridLength.Auto;
            }
            else
            {
                SeekSlider.Visibility = Visibility.Collapsed;
                SeekSliderRow.Height = new GridLength(0);
            }
        }
    }
}