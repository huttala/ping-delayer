using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Text.RegularExpressions;

namespace PingDelayer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private NetworkDelayEngine? engine;
    private DispatcherTimer? updateTimer;
    private bool isUpdatingFromSlider = false;
    private bool isUpdatingFromTextBox = false;

    public MainWindow()
    {
        InitializeComponent();
        InitializeEngine();
        InitializeTimer();
        LogMessage("Application started. Ready to add network delay.");
        LogMessage("Note: This application must run as Administrator to function properly.");
    }

    private void InitializeEngine()
    {
        engine = new NetworkDelayEngine();
        engine.StatusChanged += Engine_StatusChanged;
        engine.ErrorOccurred += Engine_ErrorOccurred;
    }

    private void InitializeTimer()
    {
        // Update UI every 100ms
        updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        updateTimer.Tick += UpdateTimer_Tick;
        updateTimer.Start();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (engine != null)
        {
            // Update queued packet count
            QueuedPacketsText.Text = engine.QueuedPacketCount.ToString();
        }
    }

    private void Engine_StatusChanged(object? sender, string message)
    {
        Dispatcher.Invoke(() =>
        {
            LogMessage(message);
            UpdateStatus();
        });
    }

    private void Engine_ErrorOccurred(object? sender, string message)
    {
        Dispatcher.Invoke(() =>
        {
            LogMessage($"ERROR: {message}");
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    private void UpdateStatus()
    {
        if (engine != null && engine.IsRunning)
        {
            StatusText.Text = "Active";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Green);
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            DelaySlider.IsEnabled = true;
            DelayTextBox.IsEnabled = true;
        }
        else
        {
            StatusText.Text = "Inactive";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            DelaySlider.IsEnabled = true;
            DelayTextBox.IsEnabled = true;
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (engine == null)
            return;

        int delay = (int)DelaySlider.Value;
        
        if (engine.Start(delay))
        {
            CurrentDelayText.Text = $"{delay} ms";
            UpdateStatus();
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (engine == null)
            return;

        engine.Stop();
        UpdateStatus();
    }

    private void DelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (isUpdatingFromTextBox)
            return;

        isUpdatingFromSlider = true;
        
        int delay = (int)DelaySlider.Value;
        DelayTextBox.Text = delay.ToString();
        
        if (engine != null && engine.IsRunning)
        {
            engine.UpdateDelay(delay);
            CurrentDelayText.Text = $"{delay} ms";
        }
        
        isUpdatingFromSlider = false;
    }

    private void DelayTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (isUpdatingFromSlider)
            return;

        isUpdatingFromTextBox = true;
        
        if (int.TryParse(DelayTextBox.Text, out int delay))
        {
            // Clamp value to valid range
            delay = Math.Max(0, Math.Min(1000, delay));
            DelaySlider.Value = delay;
            
            if (engine != null && engine.IsRunning)
            {
                engine.UpdateDelay(delay);
                CurrentDelayText.Text = $"{delay} ms";
            }
        }
        
        isUpdatingFromTextBox = false;
    }

    private void DelayTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Only allow numeric input
        e.Handled = !IsTextNumeric(e.Text);
    }

    private static bool IsTextNumeric(string text)
    {
        Regex regex = new Regex("[^0-9]+");
        return !regex.IsMatch(text);
    }

    private void LogMessage(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogTextBox.AppendText($"[{timestamp}] {message}\n");
        LogTextBox.ScrollToEnd();
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Clean shutdown
        updateTimer?.Stop();
        
        if (engine != null)
        {
            LogMessage("Shutting down engine...");
            engine.Stop();
            engine.Dispose();
        }
    }
}