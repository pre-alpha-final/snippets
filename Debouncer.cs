public class Debouncer
{
    private Func<Task>? _func = null;

    public async Task Run(Func<Task> func, int delay = 100)
    {
        _func = func;
        await Task.Delay(delay);
        if (_func == func)
        {
            await func();
        }
    }
}
