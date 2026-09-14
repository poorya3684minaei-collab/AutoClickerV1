using System.Windows;
using System.Windows.Input;

namespace AutoClickerV1;

public partial class PointPickerWindow : Window
{
    public Point? SelectedPoint { get; private set; }

    public PointPickerWindow()
    {
        InitializeComponent();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(this);
        // تبدیل به مختصات صفحه واقعی
        var screenPoint = PointToScreen(pos);
        SelectedPoint = new Point((int)screenPoint.X, (int)screenPoint.Y);
        DialogResult = true;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedPoint = null;
            DialogResult = false;
            Close();
        }
    }
}
