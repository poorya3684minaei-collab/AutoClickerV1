namespace AutoClickerV1.Models;

public enum CheckMode
{
    None = 0,
    Color = 1
}

public class ClickPoint
{
    public int Number { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    // فاصله زمانی از این نقطه تا نقطه بعدی.
    // برای آخرین نقطه، فاصله زمانی تا نقطه اولِ دور بعدی است.
    public int DelayMs { get; set; } = 1000;

    // رنگ اختصاصی همین نقطه
    public byte R { get; set; }

    public byte G { get; set; }

    public byte B { get; set; }

    // میزان اختلاف مجاز هر کانال رنگی
    // 0 = کاملاً دقیق
    // 255 = بسیار آزاد
    public int Tolerance { get; set; } = 10;

    // آیا بررسی رنگ برای این نقطه فعال است؟
    public CheckMode CheckMode { get; set; } = CheckMode.None;

    // شعاع محدوده‌ای که اطراف نقطه برای پیدا کردن رنگ بررسی می‌شود.
    // مقدار 12 یعنی محدوده 25×25 پیکسل.
    public int SearchRadius { get; set; } = 12;

    // حداقل درصد پیکسل‌های منطبق برای قبول کردن رنگ.
    // مثلاً 5 یعنی حداقل 5 درصد محدوده باید با رنگ موردنظر منطبق باشد.
    public int MinimumMatchPercent { get; set; } = 5;

    public string ColorHex =>
        $"#{R:X2}{G:X2}{B:X2}";

    public string DelayText =>
        $"{DelayMs / 1000.0:0.###}s";

    public string CheckModeText =>
        CheckMode == CheckMode.Color
            ? "بررسی رنگ"
            : "بدون بررسی";
}
