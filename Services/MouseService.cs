using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;

namespace AutoClickerV1.Services;

public class MouseService
{
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    public void Click(int x, int y)
    {
        SetCursorPos(x, y);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    public Color GetPixelColor(int x, int y)
    {
        using var bmp = new Bitmap(1, 1);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(1, 1));
        return bmp.GetPixel(0, 0);
    }

    public bool IsColorMatch(int x, int y, byte r, byte g, byte b, int tolerance)
    {
        var current = GetPixelColor(x, y);
        return Math.Abs(current.R - r) <= tolerance &&
               Math.Abs(current.G - g) <= tolerance &&
               Math.Abs(current.B - b) <= tolerance;
    }
}
