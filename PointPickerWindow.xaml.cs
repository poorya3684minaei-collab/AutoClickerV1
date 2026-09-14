using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace AutoClickerV1;

public partial class PointPickerWindow : Window
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(
        int nIndex);

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern bool GetCursorPos(
        out POINT point);

    public Point? SelectedPoint { get; private set; }

    public PointPickerWindow()
    {
        InitializeComponent();

        ConfigureVirtualDesktop();
    }

    private void ConfigureVirtualDesktop()
    {
        int left =
            GetSystemMetrics(
                SM_XVIRTUALSCREEN);

        int top =
            GetSystemMetrics(
                SM_YVIRTUALSCREEN);

        int width =
            GetSystemMetrics(
                SM_CXVIRTUALSCREEN);

        int height =
            GetSystemMetrics(
                SM_CYVIRTUALSCREEN);

        if (width <= 0)
            width = 1;

        if (height <= 0)
            height = 1;

        Left = left;
        Top = top;
        Width = width;
        Height = height;

        WindowState =
            WindowState.Normal;
    }

    private void Window_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (!GetCursorPos(
                out POINT cursor))
        {
            DialogResult = false;
            Close();
            return;
        }

        SelectedPoint =
            new Point(
                cursor.X,
                cursor.Y);

        DialogResult = true;
        Close();
    }

    private void Window_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedPoint = null;
            DialogResult = false;
            Close();
        }
    }
}
