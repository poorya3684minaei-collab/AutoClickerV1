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
    public int DelayMs { get; set; } = 1000;
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public int Tolerance { get; set; } = 10;
    public CheckMode CheckMode { get; set; } = CheckMode.None;

    public string ColorHex => $"#{R:X2}{G:X2}{B:X2}";
    public string DelayText => $"{DelayMs / 1000.0:0.###}s";
}
