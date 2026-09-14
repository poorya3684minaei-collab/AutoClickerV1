using AutoClickerV1.Models;
using AutoClickerV1.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace AutoClickerV1;

public partial class MainWindow : Window
{
    private readonly MouseService _mouse;
    private readonly SequenceRunner _runner;
    private readonly ObservableCollection<ClickPoint> _points;


    public MainWindow()
    {
        InitializeComponent();


        _mouse = new MouseService();

        _runner =
            new SequenceRunner(_mouse);

        _points =
            new ObservableCollection<ClickPoint>();


        PointsList.ItemsSource =
            _points;


        DelayBox.Text =
            "1.0";

        ToleranceBox.Text =
            "10";

        RepeatCountBox.Text =
            "1";


        InfiniteCheckBox.IsChecked =
            false;


        SetRunningState(false);


        StatusText.Text =
            "آماده";


        RefreshEditor();
    }


    private void AddPoint_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;


        try
        {
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


            var color =
                _mouse.GetPixelColor(
                    x,
                    y);


            var point =
                new ClickPoint
                {
                    Number =
                        _points.Count + 1,

                    X = x,

                    Y = y,

                    DelayMs =
                        1000,

                    R = color.R,

                    G = color.G,

                    B = color.B,

                    Tolerance =
                        10,

                    CheckMode =
                        CheckMode.None
                };


            _points.Add(point);


            PointsList.SelectedItem =
                point;


            PointsList.ScrollIntoView(
                point);


            RefreshEditor();


            StatusText.Text =
                $"نقطه {point.Number} اضافه شد | " +
                $"مختصات: ({point.X}, {point.Y}) | " +
                $"رنگ: {point.ColorHex}";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }


    private void PointsList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        RefreshEditor();
    }


    private void RefreshEditor()
    {
        if (PointsList.SelectedItem
            is not ClickPoint point)
        {
            ColorInfoText.Text =
                "—";

            DelayBox.Text =
                "1.0";

            ToleranceBox.Text =
                "10";

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


        ColorInfoText.Text =
            $"{point.ColorHex} | " +
            $"({point.X}, {point.Y})";


        int index =
            PointsList.SelectedIndex;


        if (index >= 0 &&
            index < _points.Count)
        {
            int nextIndex =
                index + 1;


            if (nextIndex >= _points.Count)
            {
                nextIndex = 0;
            }


            int nextNumber =
                _points.Count > 0
                    ? _points[nextIndex].Number
                    : 1;


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


    private void ApplyDelay_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;


        if (PointsList.SelectedItem
            is not ClickPoint point)
        {
            MessageBox.Show(
                "ابتدا یک نقطه را انتخاب کنید.",
                "توجه",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

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
            MessageBox.Show(
                "فاصله باید یک عدد معتبر باشد.",
                "خطا",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }


        if (seconds < 0)
        {
            MessageBox.Show(
                "فاصله نمی‌تواند منفی باشد.",
                "خطا",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }


        double milliseconds =
            seconds * 1000.0;


        if (milliseconds >
            int.MaxValue)
        {
            MessageBox.Show(
                "مقدار فاصله بیش از حد بزرگ است.",
                "خطا",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }


        point.DelayMs =
            (int)Math.Round(
                milliseconds);


        PointsList.Items.Refresh();


        RefreshEditor();


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


    private void EnableColorCheck_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;


        if (PointsList.SelectedItem
            is not ClickPoint point)
        {
            MessageBox.Show(
                "ابتدا یک نقطه را انتخاب کنید.",
                "توجه",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }


        string text =
            ToleranceBox.Text.Trim();


        if (!int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int tolerance))
        {
            MessageBox.Show(
                "Tolerance باید یک عدد صحیح باشد.",
                "خطا",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }


        if (tolerance < 0 ||
            tolerance > 255)
        {
            MessageBox.Show(
                "Tolerance باید بین 0 تا 255 باشد.",
                "خطا",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }


        point.Tolerance =
            tolerance;


        point.CheckMode =
            CheckMode.Color;


        PointsList.Items.Refresh();


        RefreshEditor();


        StatusText.Text =
            $"بررسی رنگ برای نقطه " +
            $"{point.Number} فعال شد.";
    }


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


    private async void Start_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;


        if (_points.Count == 0)
        {
            MessageBox.Show(
                "هیچ نقطه‌ای برای اجرا وجود ندارد.",
                "توجه",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

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
                MessageBox.Show(
                    "تعداد تکرار باید یک عدد صحیح باشد.",
                    "خطا",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            if (repeatCount < 1)
            {
                MessageBox.Show(
                    "تعداد تکرار باید حداقل 1 باشد.",
                    "خطا",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

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


    private void DeletePoint_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;


        if (PointsList.SelectedItem
            is not ClickPoint point)
        {
            MessageBox.Show(
                "ابتدا یک نقطه را انتخاب کنید.",
                "توجه",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

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
            PointsList.SelectedIndex =
                0;
        }
        else
        {
            RefreshEditor();
        }


        StatusText.Text =
            "نقطه حذف شد.";
    }


    private void EditColor_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_runner.IsRunning)
            return;


        if (PointsList.SelectedItem
            is not ClickPoint point)
        {
            MessageBox.Show(
                "ابتدا یک نقطه را انتخاب کنید.",
                "توجه",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }


        try
        {
            var color =
                _mouse.GetPixelColor(
                    point.X,
                    point.Y);


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
                $"به رنگ فعلی صفحه تغییر کرد: " +
                $"{point.ColorHex}";
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }


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


        ToleranceBox.IsEnabled =
            !running;


        InfiniteCheckBox.IsEnabled =
            !running;


        RepeatCountBox.IsEnabled =
            !running &&
            InfiniteCheckBox.IsChecked != true;
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
