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

    // فاصله زمانی بین کلیک این نقطه و کلیک نقطه بعدی.
    // برای آخرین نقطه: فاصله آخرین نقطه تا نقطه اول.
    public int DelayMs { get; set; } = 1000;

    // رنگ اختصاصی همین نقطه
    public byte R { get; set; }

    public byte G { get; set; }

    public byte B { get; set; }

    // اختلاف مجاز هر کانال رنگ
    // 0 = کاملاً دقیق
    // 255 = بسیار آزاد
    public int Tolerance { get; set; } = 20;

    // فعال یا غیرفعال بودن بررسی رنگ
    public CheckMode CheckMode { get; set; } = CheckMode.None;

    // شعاع جست‌وجوی رنگ اطراف نقطه
    public int SearchRadius { get; set; } = 15;

    // حداقل درصد پیکسل‌های مشابه
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
