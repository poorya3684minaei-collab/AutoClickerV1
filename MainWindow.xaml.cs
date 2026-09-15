using AutoClickerV1.Models;
using AutoClickerV1.Services;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace AutoClickerV1;

public partial class MainWindow : Window
{
    private readonly MouseService _mouse;
    private readonly SequenceRunner _runner;
    private readonly ObservableCollection<ClickPoint> _points;

    // ============================================================
    // Global F7 stop
    //
    // F7 is handled with a low-level keyboard hook instead of
    // RegisterHotKey. This keeps the stop key global and avoids
    // depending on a registered window hotkey.
    // ============================================================

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_F7 = 0x76;

    private LowLevelKeyboardProc? _keyboardHookProc;
    private IntPtr _keyboardHookHandle = IntPtr.Zero;

    private delegate IntPtr LowLevelKeyboardProc(
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(
        IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(
        string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    // ============================================================
    // Constructor
    // ============================================================

    public MainWindow()
    {
        InitializeComponent();

        // Main services.
        _mouse =
            new MouseService();

        _runner =
            new SequenceRunner(_mouse);

        _points =
            new ObservableCollection<ClickPoint>();

        // Bind the point collection to the list.
        PointsList.ItemsSource =
            _points;

        // Default editor values.
        DelayBox.Text =
            "1.0";

        ToleranceBox.Text =
            "20";

        SearchRadiusBox.Text =
            "15";

        MinimumMatchPercentBox.Text =
            "5";

        RepeatCountBox.Text =
            "1";

        InfiniteCheckBox.IsChecked =
            false;

        // The application starts idle.
        SetRunningState(false);

        StatusText.Text =
            "آماده";

        RefreshEditor();

        // Start the global F7 hook after the window has been
        // constructed. The hook itself is independent of focus.
        StartGlobalF7Hook();
    }

    // ============================================================
    // Global F7 hook
    // ============================================================

    private void StartGlobalF7Hook()
    {
        if (_keyboardHookHandle != IntPtr.Zero)
            return;

        // Keep a strong reference to the delegate for as long as
        // the native hook exists. Losing the delegate can cause
        // the native callback to point at collected managed data.
        _keyboardHookProc =
            GlobalKeyboardHookCallback;

        IntPtr moduleHandle =
            GetModuleHandle(null);

        _keyboardHookHandle =
            SetWindowsHookEx(
                WH_KEYBOARD_LL,
                _keyboardHookProc,
                moduleHandle,
                0);

        if (_keyboardHookHandle == IntPtr.Zero)
        {
            int error =
                Marshal.GetLastWin32Error();

            System.Diagnostics.Debug.WriteLine(
                $"F7 keyboard hook failed. Win32 error: {error}");
        }
    }

    private IntPtr GlobalKeyboardHookCallback(
        int nCode,
        IntPtr wParam,
        IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int message =
                wParam.ToInt32();

            if (message == WM_KEYDOWN ||
                message == WM_SYSKEYDOWN)
            {
                KBDLLHOOKSTRUCT keyboard =
                    Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(
                        lParam);

                if (keyboard.vkCode ==
                    (uint)VK_F7)
                {
                    // Do not touch WPF controls directly from the
                    // native callback. Queue the operation on the
                    // WPF dispatcher instead.
                    Dispatcher.BeginInvoke(
                        new Action(StopFromHotkey));
                }
            }
        }

        return CallNextHookEx(
            _keyboardHookHandle,
            nCode,
            wParam,
            lParam);
    }

    private void StopFromHotkey()
    {
        // F7 is deliberately a stop-only command.
        // Pressing it while idle does nothing.
        if (!_runner.IsRunning)
            return;

        StatusText.Text =
            "در حال توقف با F7...";

        _runner.Stop();
    }

    private void StopGlobalF7Hook()
    {
        if (_keyboardHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(
                _keyboardHookHandle);

            _keyboardHookHandle =
                IntPtr.Zero;
        }

        _keyboardHookProc =
            null;
    }

    // ============================================================
    // Window close
    // ============================================================

    protected override void OnClosed(
        EventArgs e)
    {
        // Stop the native hook first so no new F7 callback is queued
        // while the window is being destroyed.
        StopGlobalF7Hook();

        // Stop the click sequence if it is still active.
        _runner.Stop();

        base.OnClosed(e);
    }

    // ============================================================
    // Add point
    // ============================================================

    private void AddPoint_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;

        try
        {
            // The picker is shown while the main window is hidden,
            // so the application does not accidentally sample its
            // own controls.
            var picker =
                new PointPickerWindow
                {
                    Owner = this
                };

            Hide();

            bool? result =
                picker.ShowDialog();

            Show();
            Activate();
            Focus();

            if (result != true)
                return;

            if (picker.SelectedPoint
                is not System.Windows.Point selectedPoint)
                return;

            int x =
                (int)Math.Round(
                    selectedPoint.X);

            int y =
                (int)Math.Round(
                    selectedPoint.Y);

            // Store a representative color from the surrounding
            // area, not merely one potentially noisy pixel.
            Color color =
                _mouse.GetRepresentativeColor(
                    x,
                    y,
                    15);

            var point =
                new ClickPoint
                {
                    Number =
                        _points.Count + 1,

                    X =
                        x,

                    Y =
                        y,

                    DelayMs =
                        1000,

                    R =
                        color.R,

                    G =
                        color.G,

                    B =
                        color.B,

                    Tolerance =
                        20,

                    CheckMode =
                        CheckMode.None,

                    SearchRadius =
                        15,

                    MinimumMatchPercent =
                        5
                };

            _points.Add(
                point);

            PointsList.SelectedItem =
                point;

            PointsList.ScrollIntoView(
                point);

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

    // ============================================================
    // Selection changed
    // ============================================================

    private void PointsList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        RefreshEditor();
    }

    // ============================================================
    // Refresh editor
    // ============================================================

    private void RefreshEditor()
    {
        // No point selected.
        if (PointsList.SelectedItem
            is not ClickPoint point)
        {
            ColorInfoText.Text =
                "—";

            DelayBox.Text =
                "1.0";

            ToleranceBox.Text =
                "20";

            SearchRadiusBox.Text =
                "15";

            MinimumMatchPercentBox.Text =
                "5";

            DelayLabel.Text =
                "فاصله کلیک → کلیک بعدی (ثانیه)";

            return;
        }

        // Convert milliseconds back to seconds for the UI.
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
            // The selected point owns the delay before the next
            // point. For the final point, the next point is point 1.
            int nextIndex =
                index + 1;

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

    // ============================================================
    // Apply delay
    // ============================================================

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
                .Replace(
                    ',',
                    '.');

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

        if (milliseconds >
            int.MaxValue)
        {
            ShowWarning(
                "مقدار فاصله بیش از حد بزرگ است.");

            return;
        }

        point.DelayMs =
            (int)Math.Round(
                milliseconds);

        PointsList.Items.Refresh();

        RefreshEditor();

        // Calculate the exact destination of this delay.
        int index =
            PointsList.SelectedIndex;

        int nextNumber =
            point.Number + 1;

        if (index >= 0 &&
            index < _points.Count)
        {
            if (index + 1 <
                _points.Count)
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

    // ============================================================
    // Enable color check
    // ============================================================

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

    // ============================================================
    // Disable color check
    // ============================================================

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

    // ============================================================
    // Read color settings
    // ============================================================

    private bool TryReadColorSettings(
        out int tolerance,
        out int searchRadius,
        out int minimumMatchPercent)
    {
        tolerance =
            0;

        searchRadius =
            15;

        minimumMatchPercent =
            5;

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

    // ============================================================
    // Start
    // ============================================================

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

        int repeatCount =
            1;

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
            SetRunningState(
                true);

            StatusText.Text =
                "در حال اجرا...";

            // Use a snapshot so the UI collection cannot be changed
            // while the runner is processing the sequence.
            var pointsSnapshot =
                _points.ToList();

            await _runner.StartAsync(
                pointsSnapshot,
                repeatCount,
                infinite,
                status =>
                {
                    Dispatcher.Invoke(
                        () =>
                        {
                            StatusText.Text =
                                status;
                        });
                },
                point =>
                {
                    Dispatcher.Invoke(
                        () =>
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
            ShowError(
                ex);
        }
        finally
        {
            SetRunningState(
                false);

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

    // ============================================================
    // Stop button
    // ============================================================

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

    // ============================================================
    // Delete point
    // ============================================================

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

        _points.Remove(
            point);

        // Renumber the remaining points.
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

    // ============================================================
    // Edit / recapture point color
    // ============================================================

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

            point.R =
                color.R;

            point.G =
                color.G;

            point.B =
                color.B;

            PointsList.Items.Refresh();

            RefreshEditor();

            StatusText.Text =
                $"رنگ نقطه {point.Number} " +
                $"دوباره از محدوده صفحه ثبت شد: " +
                $"{point.ColorHex}";
        }
        catch (Exception ex)
        {
            ShowError(
                ex);
        }
    }

    // ============================================================
    // Infinite mode
    // ============================================================

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

    // ============================================================
    // Running-state UI
    // ============================================================

    private void SetRunningState(
        bool running)
    {
        // Point management.
        AddPointButton.IsEnabled =
            !running;

        DeletePointButton.IsEnabled =
            !running;

        EditColorButton.IsEnabled =
            !running;

        // Start/stop.
        StartButton.IsEnabled =
            !running;

        StopButton.IsEnabled =
            running;

        // Editor.
        DelayBox.IsEnabled =
            !running;

        ToleranceBox.IsEnabled =
            !running;

        SearchRadiusBox.IsEnabled =
            !running;

        MinimumMatchPercentBox.IsEnabled =
            !running;

        // Repeat settings.
        InfiniteCheckBox.IsEnabled =
            !running;

        RepeatCountBox.IsEnabled =
            !running &&
            InfiniteCheckBox.IsChecked != true;
    }

    // ============================================================
    // Message helpers
    // ============================================================

    private void ShowInformation(
        string message)
    {
        MessageBox.Show(
            message,
            "توجه",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ShowWarning(
        string message)
    {
        MessageBox.Show(
            message,
            "خطا",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void ShowError(
        Exception ex)
    {
        Show();

        MessageBox.Show(
            ex.Message,
            "خطا",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        StatusText.Text =
            "خطا";
    }

}
