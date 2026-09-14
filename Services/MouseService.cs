using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AutoClickerV1.Services;

public class MouseService
{
    private const uint INPUT_MOUSE = 0;

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern bool SetCursorPos(
        int X,
        int Y);

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    private static extern uint SendInput(
        uint nInputs,
        INPUT[] pInputs,
        int cbSize);


    public void MoveTo(int x, int y)
    {
        if (!SetCursorPos(x, y))
        {
            throw new InvalidOperationException(
                "امکان جابه‌جایی نشانگر ماوس وجود ندارد.");
        }
    }


    public void Click(int x, int y)
    {
        MoveTo(x, y);

        var inputs = new INPUT[2];

        inputs[0] = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTDOWN,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        inputs[1] = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = 0,
                    dy = 0,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_LEFTUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        uint sent = SendInput(
            (uint)inputs.Length,
            inputs,
            Marshal.SizeOf<INPUT>());

        if (sent != inputs.Length)
        {
            throw new InvalidOperationException(
                "ارسال کلیک به Windows ناموفق بود.");
        }
    }


    public Color GetPixelColor(int x, int y)
    {
        using var bitmap = new Bitmap(1, 1);

        using var graphics =
            Graphics.FromImage(bitmap);

        graphics.CopyFromScreen(
            x,
            y,
            0,
            0,
            new System.Drawing.Size(1, 1));

        return bitmap.GetPixel(0, 0);
    }


    public bool IsColorMatch(
        int x,
        int y,
        byte r,
        byte g,
        byte b,
        int tolerance)
    {
        if (tolerance < 0)
            tolerance = 0;

        if (tolerance > 255)
            tolerance = 255;

        Color current =
            GetPixelColor(x, y);

        return
            Math.Abs(current.R - r) <= tolerance &&
            Math.Abs(current.G - g) <= tolerance &&
            Math.Abs(current.B - b) <= tolerance;
    }
}
