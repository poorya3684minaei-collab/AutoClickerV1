using AutoClickerV1.Models;

namespace AutoClickerV1.Services;

public class SequenceRunner
{
    private readonly MouseService _mouse;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

    public SequenceRunner(MouseService mouse)
    {
        _mouse = mouse;
    }

    public async Task StartAsync(
        List<ClickPoint> points,
        int repeatCount,
        bool infinite,
        Action<string> statusCallback,
        Action<ClickPoint> currentPointCallback)
    {
        if (points.Count == 0)
        {
            statusCallback("هیچ نقطه‌ای وجود ندارد.");
            return;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            int loop = 0;
            while (infinite || loop < repeatCount)
            {
                loop++;
                statusCallback(infinite ? $"تکرار نامحدود - دور {loop}" : $"دور {loop} از {repeatCount}");

                foreach (var point in points)
                {
                    token.ThrowIfCancellationRequested();

                    currentPointCallback(point);
                    statusCallback($"کلیک روی نقطه {point.Number} → ({point.X}, {point.Y})");

                    bool shouldClick = true;

                    if (point.CheckMode == CheckMode.Color)
                    {
                        shouldClick = _mouse.IsColorMatch(
                            point.X, point.Y,
                            point.R, point.G, point.B,
                            point.Tolerance);

                        if (!shouldClick)
                            statusCallback($"نقطه {point.Number}: رنگ مطابقت نداشت → رد شد");
                    }

                    if (shouldClick)
                        _mouse.Click(point.X, point.Y);

                    await Task.Delay(point.DelayMs, token);
                }
            }
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}
