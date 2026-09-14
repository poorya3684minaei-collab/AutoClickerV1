using AutoClickerV1.Models;

namespace AutoClickerV1.Services;

public class SequenceRunner
{
    private readonly MouseService _mouse;

    private CancellationTokenSource? _cts;

    public bool IsRunning
    {
        get
        {
            return _cts != null &&
                   !_cts.IsCancellationRequested;
        }
    }


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
        if (points == null || points.Count == 0)
        {
            statusCallback("هیچ نقطه‌ای وجود ندارد.");
            return;
        }

        if (!infinite && repeatCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(repeatCount),
                "تعداد تکرار باید حداقل 1 باشد.");
        }

        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();

        CancellationToken token = _cts.Token;

        try
        {
            int loop = 0;

            while (infinite || loop < repeatCount)
            {
                token.ThrowIfCancellationRequested();

                loop++;

                if (infinite)
                {
                    statusCallback(
                        $"تکرار نامحدود - دور {loop}");
                }
                else
                {
                    statusCallback(
                        $"دور {loop} از {repeatCount}");
                }


                foreach (ClickPoint point in points)
                {
                    token.ThrowIfCancellationRequested();

                    currentPointCallback(point);

                    statusCallback(
                        $"بررسی نقطه {point.Number} | ({point.X}, {point.Y})");


                    bool shouldClick = true;


                    if (point.CheckMode == CheckMode.Color)
                    {
                        bool colorMatches =
                            _mouse.IsColorMatch(
                                point.X,
                                point.Y,
                                point.R,
                                point.G,
                                point.B,
                                point.Tolerance);

                        if (!colorMatches)
                        {
                            shouldClick = false;

                            statusCallback(
                                $"نقطه {point.Number}: رنگ مطابقت ندارد → کلیک رد شد.");
                        }
                    }


                    if (shouldClick)
                    {
                        statusCallback(
                            $"کلیک روی نقطه {point.Number} → ({point.X}, {point.Y})");

                        _mouse.Click(
                            point.X,
                            point.Y);
                    }


                    if (point.DelayMs > 0)
                    {
                        await Task.Delay(
                            point.DelayMs,
                            token);
                    }
                }
            }
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
        }
    }


    public void Stop()
    {
        _cts?.Cancel();
    }
}
