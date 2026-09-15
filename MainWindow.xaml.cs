using AutoClickerV1.Models;
using AutoClickerV1.Services;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace AutoClickerV1;

public partial class MainWindow : Window
{
    private readonly MouseService _mouse;
    private readonly SequenceRunner _runner;
    private readonly ObservableCollection<ClickPoint> _points;

    // ==============================
    // Global F7 Stop Hotkey
    // ==============================

    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_F7 = 0x76;
    private const int HOTKEY_ID_STOP = 1001;

    private HwndSource? _hwndSource;
    private bool _hotkeyRegistered;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);

    // ==============================
    // Constructor
    // ==============================

    public MainWindow()
    {
        InitializeComponent();

        _mouse = new MouseService();
        _runner = new SequenceRunner(_mouse);
        _points = new ObservableCollection<ClickPoint>();

        PointsList.ItemsSource = _points;

        DelayBox.Text = "1.0";
        ToleranceBox.Text = "20";
        SearchRadiusBox.Text = "15";
        MinimumMatchPercentBox.Text = "5";
        RepeatCountBox.Text = "1";

        InfiniteCheckBox.IsChecked = false;

        SetRunningState(false);

        StatusText.Text = "آماده";

        RefreshEditor();

        SourceInitialized += MainWindow_SourceInitialized;
    }

    // ==============================
    // Register F7
    // ==============================

    private void MainWindow_SourceInitialized(
        object? sender,
        EventArgs e)
    {
        if (_hwndSource != null)
            return;

        _hwndSource =
            PresentationSource.FromVisual(this)
            as HwndSource;

        if (_hwndSource == null)
            return;

        _hwndSource.AddHook(GlobalHotkeyWndProc);

        _hotkeyRegistered = RegisterHotKey(
            _hwndSource.Handle,
            HOTKEY_ID_STOP,
            MOD_NOREPEAT,
            VK_F7);

        if (!_hotkeyRegistered)
        {
            int error = Marshal.GetLastWin32Error();

            System.Diagnostics.Debug.WriteLine(
                $"RegisterHotKey(F7) failed. Win32 error: {error}");
        }
    }

    // ==============================
    // Global Hotkey Handler
    // ==============================

    private IntPtr GlobalHotkeyWndProc(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (msg == WM_HOTKEY &&
            wParam.ToInt32() == HOTKEY_ID_STOP)
        {
            handled = true;

            Dispatcher.BeginInvoke(
                new Action(StopFromHotkey));
        }

        return IntPtr.Zero;
    }

    private void StopFromHotkey()
    {
        if (!_runner.IsRunning)
            return;

        StatusText.Text = "در حال توقف با F7...";

        _runner.Stop();
    }

    // ==============================
    // Window Close
    // ==============================

    protected override void OnClosed(EventArgs e)
    {
        if (_hwndSource != null)
        {
            if (_hotkeyRegistered)
            {
                UnregisterHotKey(
                    _hwndSource.Handle,
                    HOTKEY_ID_STOP);

                _hotkeyRegistered = false;
            }

            _hwndSource.RemoveHook(
                GlobalHotkeyWndProc);

            _hwndSource = null;
        }

        _runner.Stop();

        base.OnClosed(e);
    }

    // ==============================
    // Add Point
    // ==============================

    private void AddPoint_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;

        try
        {
            var picker = new PointPickerWindow
            {
                Owner = this
            };

            Hide();

            bool? result = picker.ShowDialog();

            Show();
            Activate();
            Focus();

            if (result != true)
                return;

            if (picker.SelectedPoint
                is not System.Windows.Point selectedPoint)
                return;

            int x = (int)Math.Round(selectedPoint.X);
            int y = (int)Math.Round(selectedPoint.Y);

            Color color =
                _mouse.GetRepresentativeColor(
                    x,
                    y,
                    15);

            var point = new ClickPoint
            {
                Number = _points.Count + 1,

                X = x,
                Y = y,

                DelayMs = 1000,

                R = color.R,
                G = color.G,
                B = color.B,

                Tolerance = 20,

                CheckMode = CheckMode.None,

                SearchRadius = 15,

                MinimumMatchPercent = 5
            };

            _points.Add(point);

            PointsList.SelectedItem = point;

            PointsList.ScrollIntoView(point);

            RefreshEditor();

            StatusText.Text =
                $"نقطه {point.Number} اضافه شد | " +
                $"مختصات: ({point.X}, {point.Y}) | " +
                $"رنگ نماینده: {point.ColorHex}";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    // ==============================
    // Selection Changed
    // ==============================

    private void PointsList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        RefreshEditor();
    }

    // ==============================
    // Refresh Editor
    // ==============================

    private void RefreshEditor()
    {
        if (PointsList.SelectedItem
            is not ClickPoint point)
        {
            ColorInfoText.Text = "—";

            DelayBox.Text = "1.0";

            ToleranceBox.Text = "20";

            SearchRadiusBox.Text = "15";

            MinimumMatchPercentBox.Text = "5";

            DelayLabel.Text =
                "فاصله کلیک → کلیک بعدی (ثانیه)";

            return;
        }

        DelayBox.Text =
            (point.DelayMs / 1000.0)
            .ToString(
                "0.###",
                CultureInfo.InvariantCulture);

        ToleranceBox.Text =
            point.Tolerance.ToString(
                CultureInfo.InvariantCulture);

        SearchRadiusBox.Text =
            point.SearchRadius.ToString(
                CultureInfo.InvariantCulture);

        MinimumMatchPercentBox.Text =
            point.MinimumMatchPercent.ToString(
                CultureInfo.InvariantCulture);

        ColorInfoText.Text =
            $"{point.ColorHex} | " +
            $"({point.X}, {point.Y}) | " +
            $"{point.CheckModeText}";

        int index =
            PointsList.SelectedIndex;

        if (index >= 0 &&
            index < _points.Count)
        {
            int nextIndex = index + 1;

            if (nextIndex >= _points.Count)
                nextIndex = 0;

            int nextNumber =
                _points[nextIndex].Number;

            DelayLabel.Text =
                $"فاصله کلیک {point.Number} → " +
                $"کلیک {nextNumber} (ثانیه)";
        }
        else
        {
            DelayLabel.Text =
                $"فاصله کلیک {point.Number} → " +
                $"کلیک بعدی (ثانیه)";
        }
    }

    // ==============================
    // Apply Delay
    // ==============================

    private void ApplyDelay_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;

        if (PointsList.SelectedItem
            is not ClickPoint point)
        {
            ShowInformation(
                "ابتدا یک نقطه را انتخاب کنید.");

            return;
        }

        string text =
            DelayBox.Text
            .Trim()
            .Replace(',', '.');

        if (!double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double seconds))
        {
            ShowWarning(
                "فاصله باید یک عدد معتبر باشد.");

            return;
        }

        if (double.IsNaN(seconds) ||
            double.IsInfinity(seconds))
        {
            ShowWarning(
                "فاصله واردشده معتبر نیست.");

            return;
        }

        if (seconds < 0)
        {
            ShowWarning(
                "فاصله نمی‌تواند منفی باشد.");

            return;
        }

        double milliseconds =
            seconds * 1000.0;

        if (milliseconds > int.MaxValue)
        {
            ShowWarning(
                "مقدار فاصله بیش از حد بزرگ است.");

            return;
        }

        point.DelayMs =
            (int)Math.Round(milliseconds);

        PointsList.Items.Refresh();

        RefreshEditor();

        int index =
            PointsList.SelectedIndex;

        int nextNumber =
            point.Number + 1;

        if (index >= 0 &&
            index < _points.Count)
        {
            if (index + 1 < _points.Count)
            {
                nextNumber =
                    _points[index + 1].Number;
            }
            else
            {
                nextNumber =
                    _points[0].Number;
            }
        }

        StatusText.Text =
            $"فاصله کلیک {point.Number} → " +
            $"کلیک {nextNumber} " +
            $"به {point.DelayText} تغییر کرد.";
    }

    // ==============================
    // Enable Color Check
    // ==============================

    private void EnableColorCheck_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;

        if (PointsList.SelectedItem
            is not ClickPoint point)
        {
            ShowInformation(
                "ابتدا یک نقطه را انتخاب کنید.");

            return;
        }

        if (!TryReadColorSettings(
                out int tolerance,
                out int searchRadius,
                out int minimumMatchPercent))
        {
            return;
        }

        point.Tolerance =
            tolerance;

        point.SearchRadius =
            searchRadius;

        point.MinimumMatchPercent =
            minimumMatchPercent;

        point.CheckMode =
            CheckMode.Color;

        PointsList.Items.Refresh();

        RefreshEditor();

        StatusText.Text =
            $"بررسی رنگ نقطه {point.Number} فعال شد | " +
            $"Tolerance={tolerance} | " +
            $"شعاع={searchRadius} | " +
            $"حداقل تطابق={minimumMatchPercent}%";
    }

    // ==============================
    // Disable Color Check
    // ==============================

    private void DisableColorCheck_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;

        if (PointsList.SelectedItem
            is not ClickPoint point)
            return;

        point.CheckMode =
            CheckMode.None;

        PointsList.Items.Refresh();

        RefreshEditor();

        StatusText.Text =
            $"بررسی رنگ برای نقطه " +
            $"{point.Number} غیرفعال شد.";
    }

    // ==============================
    // Read Color Settings
    // ==============================

    private bool TryReadColorSettings(
        out int tolerance,
        out int searchRadius,
        out int minimumMatchPercent)
    {
        tolerance = 0;
        searchRadius = 15;
        minimumMatchPercent = 5;

        string toleranceText =
            ToleranceBox.Text.Trim();

        if (!int.TryParse(
                toleranceText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out tolerance))
        {
            ShowWarning(
                "Tolerance باید یک عدد صحیح باشد.");

            return false;
        }

        if (tolerance < 0 ||
            tolerance > 255)
        {
            ShowWarning(
                "Tolerance باید بین 0 تا 255 باشد.");

            return false;
        }

        string radiusText =
            SearchRadiusBox.Text.Trim();

        if (!int.TryParse(
                radiusText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out searchRadius))
        {
            ShowWarning(
                "شعاع جست‌وجو باید یک عدد صحیح باشد.");

            return false;
        }

        if (searchRadius < 1 ||
            searchRadius > 100)
        {
            ShowWarning(
                "شعاع جست‌وجو باید بین 1 تا 100 پیکسل باشد.");

            return false;
        }

        string percentText =
            MinimumMatchPercentBox.Text.Trim();

        if (!int.TryParse(
                percentText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out minimumMatchPercent))
        {
            ShowWarning(
                "درصد تطابق باید یک عدد صحیح باشد.");

            return false;
        }

        if (minimumMatchPercent < 0 ||
            minimumMatchPercent > 100)
        {
            ShowWarning(
                "درصد تطابق باید بین 0 تا 100 باشد.");

            return false;
        }

        return true;
    }

    // ==============================
    // Start
    // ==============================

    private async void Start_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;

        if (_points.Count == 0)
        {
            ShowInformation(
                "هیچ نقطه‌ای برای اجرا وجود ندارد.");

            return;
        }

        bool infinite =
            InfiniteCheckBox.IsChecked == true;

        int repeatCount = 1;

        if (!infinite)
        {
            if (!int.TryParse(
                    RepeatCountBox.Text.Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out repeatCount))
            {
                ShowWarning(
                    "تعداد تکرار باید یک عدد صحیح باشد.");

                return;
            }

            if (repeatCount < 1)
            {
                ShowWarning(
                    "تعداد تکرار باید حداقل 1 باشد.");

                return;
            }
        }

        try
        {
            SetRunningState(true);

            StatusText.Text =
                "در حال اجرا...";

            var pointsSnapshot =
                _points.ToList();

            await _runner.StartAsync(
                pointsSnapshot,
                repeatCount,
                infinite,

                status =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text =
                            status;
                    });
                },

                point =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        PointsList.SelectedItem =
                            point;

                        PointsList.ScrollIntoView(
                            point);
                    });
                });
        }
        catch (OperationCanceledException)
        {
            StatusText.Text =
                "اجرا متوقف شد.";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetRunningState(false);

            if (!_runner.IsRunning)
            {
                if (StatusText.Text ==
                    "در حال اجرا...")
                {
                    StatusText.Text =
                        "اجرا تمام شد.";
                }
            }
        }
    }

    // ==============================
    // Stop Button
    // ==============================

    private void Stop_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_runner.IsRunning)
        {
            StatusText.Text =
                "برنامه در حال اجرا نیست.";

            return;
        }

        StatusText.Text =
            "در حال توقف...";

        _runner.Stop();
    }

    // ==============================
    // Delete Point
    // ==============================

    private void DeletePoint_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;

        if (PointsList.SelectedItem
            is not ClickPoint point)
        {
            ShowInformation(
                "ابتدا یک نقطه را انتخاب کنید.");

            return;
        }

        _points.Remove(point);

        for (int i = 0;
             i < _points.Count;
             i++)
        {
            _points[i].Number =
                i + 1;
        }

        PointsList.Items.Refresh();

        if (_points.Count > 0)
        {
            int newIndex =
                PointsList.SelectedIndex;

            if (newIndex < 0)
                newIndex = 0;

            if (newIndex >= _points.Count)
                newIndex =
                    _points.Count - 1;

            PointsList.SelectedIndex =
                newIndex;
        }
        else
        {
            RefreshEditor();
        }

        StatusText.Text =
            "نقطه حذف شد.";
    }

    // ==============================
    // Edit Color
    // ==============================

    private void EditColor_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;

        if (PointsList.SelectedItem
            is not ClickPoint point)
        {
            ShowInformation(
                "ابتدا یک نقطه را انتخاب کنید.");

            return;
        }

        try
        {
            int radius =
                point.SearchRadius;

            if (radius < 2)
                radius = 2;

            Color color =
                _mouse.GetRepresentativeColor(
                    point.X,
                    point.Y,
                    radius);

            point.R = color.R;
            point.G = color.G;
            point.B = color.B;

            PointsList.Items.Refresh();

            RefreshEditor();

            StatusText.Text =
                $"رنگ نقطه {point.Number} " +
                $"دوباره از محدوده صفحه ثبت شد: " +
                $"{point.ColorHex}";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    // ==============================
    // Infinite
    // ==============================

    private void Infinite_Checked(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;

        RepeatCountBox.IsEnabled =
            false;
    }

    private void Infinite_Unchecked(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;

        RepeatCountBox.IsEnabled =
            true;
    }

    // ==============================
    // Running State
    // ==============================

    private void SetRunningState(
        bool running)
    {
        AddPointButton.IsEnabled =
            !running;

        DeletePointButton.IsEnabled =
            !running;

        EditColorButton.IsEnabled =
            !running;

        StartButton.IsEnabled =
            !running;

        StopButton.IsEnabled =
            running;

        DelayBox.IsEnabled =
            !running;

   
