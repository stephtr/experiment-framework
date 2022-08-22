namespace ExperimentFramework;

public static class TaskUtils
{
    public static Action<T1, T2> Debounce<T1, T2>(this Action<T1, T2> action, int milliseconds = 300) // taken from https://stackoverflow.com/questions/28472205/c-sharp-event-debounce/59296962#59296962
    {
        CancellationTokenSource? lastCToken = null;

        return (T1 a, T2 b) =>
        {
            //Cancel/dispose previous
            lastCToken?.Cancel();
            try
            {
                lastCToken?.Dispose();
            }
            catch { }

            var tokenSrc = lastCToken = new CancellationTokenSource();

            Task.Delay(milliseconds).ContinueWith(task => { action(a, b); }, tokenSrc.Token);
        };
    }
}
