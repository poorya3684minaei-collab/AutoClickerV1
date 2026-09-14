using AutoClickerV1.Models;

namespace AutoClickerV1.Services;

public class SequenceRunner
{
    private readonly MouseService _mouse;

    private CancellationTokenSource? _cts;

    private readonly object _lock =
        new();


    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _cts != null;
            }
        }
    }


    public SequenceRunner(
        MouseService mouse)
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
        if (points == null ||
            points.Count == 0)
        {
            statusCallback(
                "هیچ نقطه‌ای وجود ندارد.");

            return;
        }


        if (!infinite &&
            repeatCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(repeatCount),
                "تعداد تکرار باید حداقل 1 باشد.");
        }


        CancellationTokenSource cts;

        lock (_lock)
        {
            if (_cts != null)
                return;

            _cts =
                new CancellationTokenSource();

            cts = _cts;
        }


        CancellationToken token =
            cts.Token;


        try
        {
            int loop = 0;


            while (infinite ||
                   loop < repeatCount)
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


                for (int index = 0;
                     index < points.Count;
                     index++)
                {
                    token.ThrowIfCancellationRequested();


                    ClickPoint point =
                        points[index];


                    currentPointCallback(point);


                    statusCallback(
                        $"بررسی نقطه {point.Number} | " +
                        $"({point.X}, {point.Y})");


                    bool shouldClick = true;


                    if (point.CheckMode ==
                        CheckMode.Color)
                    {
                        bool colorMatches =
                            _mouse.IsColorMatch(
                                point.X,
                                point.Y,
                                point.R,
                                point.G,
                                point.B,
                                point.Tolerance);


                        token.ThrowIfCancellationRequested();


                        if (!colorMatches)
                        {
                            shouldClick = false;


                            statusCallback(
                                $"نقطه {point.Number}: " +
                                $"رنگ مطابقت ندارد → " +
                                $"کلیک رد شد.");
                        }
                    }


                    if (shouldClick)
                    {
                        token.ThrowIfCancellationRequested();


                        statusCallback(
                            $"کلیک روی نقطه {point.Number} → " +
                            $"({point.X}, {point.Y})");


                        _mouse.Click(
                            point.X,
                            point.Y);
                    }


                    /*
                     * DelayMs فاصله بین این کلیک و کلیک بعدی است.
                     *
                     * اگر نقطه آخر نباشد:
                     *     فاصله تا نقطه بعدی همین دور.
                     *
                     * اگر نقطه آخر باشد:
                     *     فقط وقتی صبر می‌کنیم که دور بعدی
                     *     واقعاً قرار است اجرا شود.
                     *
                     * بنابراین در آخرین دورِ یک اجرای محدود،
                     * بعد از آخرین کلیک دیگر هیچ تأخیری نداریم.
                     */

                    bool isLastPoint =
                        index == points.Count - 1;


                    bool anotherCycleWillStart =
                        infinite ||
                        loop < repeatCount;


                    bool shouldWait =
                        !isLastPoint ||
                        anotherCycleWillStart;


                    if (shouldWait &&
                        point.DelayMs > 0)
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
            lock (_lock)
            {
                if (ReferenceEquals(
                        _cts,
                        cts))
                {
                    _cts = null;
                }
            }


            cts.Dispose();
        }
    }


    public void Stop()
    {
        CancellationTokenSource? cts;


        lock (_lock)
        {
            cts = _cts;
        }


        cts?.Cancel();
    }
}
