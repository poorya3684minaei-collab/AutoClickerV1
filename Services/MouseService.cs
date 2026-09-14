using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AutoClickerV1.Services;

public class MouseService
{
    private const uint INPUT_MOUSE = 0;

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

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

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
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
    private static extern bool GetCursorPos(
        out POINT point);

    [DllImport(
        "user32.dll")]
    private static extern int GetSystemMetrics(
        int nIndex);

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
            Marshal.SizeOf(typeof(INPUT)));

        if (sent != inputs.Length)
        {
            throw new InvalidOperationException(
                "ارسال کلیک به Windows ناموفق بود.");
        }
    }

    public Color GetPixelColor(int x, int y)
    {
        using var bitmap =
            new Bitmap(1, 1);

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

    /// <summary>
    /// رنگ نماینده محدوده اطراف نقطه را پیدا می‌کند.
    /// برای حباب‌های رنگی مثل زرد مناسب‌تر از یک پیکسل منفرد است.
    /// </summary>
    public Color GetRepresentativeColor(
        int x,
        int y,
        int searchRadius)
    {
        if (searchRadius < 2)
            searchRadius = 2;

        if (searchRadius > 100)
            searchRadius = 100;

        GetVirtualScreenBounds(
            out int virtualLeft,
            out int virtualTop,
            out int virtualWidth,
            out int virtualHeight);

        int requestedLeft =
            x - searchRadius;

        int requestedTop =
            y - searchRadius;

        int requestedRight =
            x + searchRadius;

        int requestedBottom =
            y + searchRadius;

        int left =
            Math.Max(
                requestedLeft,
                virtualLeft);

        int top =
            Math.Max(
                requestedTop,
                virtualTop);

        int right =
            Math.Min(
                requestedRight,
                virtualLeft + virtualWidth - 1);

        int bottom =
            Math.Min(
                requestedBottom,
                virtualTop + virtualHeight - 1);

        if (right < left ||
            bottom < top)
        {
            return GetPixelColor(x, y);
        }

        int width =
            right - left + 1;

        int height =
            bottom - top + 1;

        using var bitmap =
            new Bitmap(width, height);

        using var graphics =
            Graphics.FromImage(bitmap);

        graphics.CopyFromScreen(
            left,
            top,
            0,
            0,
            new System.Drawing.Size(
                width,
                height));

        long totalR = 0;
        long totalG = 0;
        long totalB = 0;

        int count = 0;

        int centerX =
            x - left;

        int centerY =
            y - top;

        // ابتدا پیکسل‌های نسبتاً اشباع را بررسی می‌کنیم.
        // این کار باعث می‌شود حباب زرد از زمینه سفید/خاکستری
        // بهتر تشخیص داده شود.
        long saturatedR = 0;
        long saturatedG = 0;
        long saturatedB = 0;
        int saturatedCount = 0;

        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                Color c =
                    bitmap.GetPixel(px, py);

                totalR += c.R;
                totalG += c.G;
                totalB += c.B;
                count++;

                int max =
                    Math.Max(
                        c.R,
                        Math.Max(c.G, c.B));

                int min =
                    Math.Min(
                        c.R,
                        Math.Min(c.G, c.B));

                int saturation =
                    max - min;

                if (saturation >= 45)
                {
                    saturatedR += c.R;
                    saturatedG += c.G;
                    saturatedB += c.B;
                    saturatedCount++;
                }
            }
        }

        if (saturatedCount >= 3)
        {
            return Color.FromArgb(
                255,
                (int)(saturatedR / saturatedCount),
                (int)(saturatedG / saturatedCount),
                (int)(saturatedB / saturatedCount));
        }

        if (count > 0)
        {
            return Color.FromArgb(
                255,
                (int)(totalR / count),
                (int)(totalG / count),
                (int)(totalB / count));
        }

        return GetPixelColor(x, y);
    }

    /// <summary>
    /// بررسی وجود رنگ در محدوده اطراف نقطه.
    /// محدوده با مرز واقعی دسکتاپ مجازی Windows قطع می‌شود.
    /// </summary>
    public bool IsColorMatchInRegion(
        int x,
        int y,
        byte r,
        byte g,
        byte b,
        int tolerance,
        int searchRadius,
        int minimumMatchPercent)
    {
        tolerance =
            Math.Max(
                0,
                Math.Min(255, tolerance));

        searchRadius =
            Math.Max(
                1,
                Math.Min(100, searchRadius));

        minimumMatchPercent =
            Math.Max(
                0,
                Math.Min(100, minimumMatchPercent));

        GetVirtualScreenBounds(
            out int virtualLeft,
            out int virtualTop,
            out int virtualWidth,
            out int virtualHeight);

        int requestedLeft =
            x - searchRadius;

        int requestedTop =
            y - searchRadius;

        int requestedRight =
            x + searchRadius;

        int requestedBottom =
            y + searchRadius;

        int left =
            Math.Max(
                requestedLeft,
                virtualLeft);

        int top =
            Math.Max(
                requestedTop,
                virtualTop);

        int right =
            Math.Min(
                requestedRight,
                virtualLeft + virtualWidth - 1);

        int bottom =
            Math.Min(
                requestedBottom,
                virtualTop + virtualHeight - 1);

        if (right < left ||
            bottom < top)
        {
            return false;
        }

        int width =
            right - left + 1;

        int height =
            bottom - top + 1;

        using var bitmap =
            new Bitmap(width, height);

        using var graphics =
            Graphics.FromImage(bitmap);

        try
        {
            graphics.CopyFromScreen(
                left,
                top,
                0,
                0,
                new System.Drawing.Size(
                    width,
                    height));

            int matchingPixels = 0;
            int totalPixels = 0;

            for (int py = 0;
                 py < height;
                 py++)
            {
                for (int px = 0;
                     px < width;
                     px++)
                {
                    Color current =
                        bitmap.GetPixel(
                            px,
                            py);

                    totalPixels++;

                    int dr =
                        Math.Abs(
                            current.R - r);

                    int dg =
                        Math.Abs(
                            current.G - g);

                    int db =
                        Math.Abs(
                            current.B - b);

                    if (dr <= tolerance &&
                        dg <= tolerance &&
                        db <= tolerance)
                    {
                        matchingPixels++;
                    }
                }
            }

            if (totalPixels == 0)
                return false;

            double percentage =
                matchingPixels * 100.0 /
                totalPixels;

            return percentage >=
                   minimumMatchPercent;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// نسخه تک‌پیکسلی برای سازگاری.
    /// </summary>
    public bool IsColorMatch(
        int x,
        int y,
        byte r,
        byte g,
        byte b,
        int tolerance)
    {
        tolerance =
            Math.Max(
                0,
                Math.Min(255, tolerance));

        Color current =
            GetPixelColor(x, y);

        return
            Math.Abs(current.R - r) <= tolerance &&
            Math.Abs(current.G - g) <= tolerance &&
            Math.Abs(current.B - b) <= tolerance;
    }

    public bool TryGetCursorPosition(
        out int x,
        out int y)
    {
        if (GetCursorPos(out POINT point))
        {
            x = point.X;
            y = point.Y;
            return true;
        }

        x = 0;
        y = 0;

        return false;
    }

    private static void GetVirtualScreenBounds(
        out int left,
        out int top,
        out int width,
        out int height)
    {
        left =
            GetSystemMetrics(
                SM_XVIRTUALSCREEN);

        top =
            GetSystemMetrics(
                SM_YVIRTUALSCREEN);

        width =
            GetSystemMetrics(
                SM_CXVIRTUALSCREEN);

        height =
            GetSystemMetrics(
                SM_CYVIRTUALSCREEN);

        if (width <= 0)
            width = 1;

        if (height <= 0)
            height = 1;
    }
}
