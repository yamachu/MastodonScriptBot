static int i = 0;

IDisposable WithMethod()
{
    return MSStream.OfType<Status>()
    .Subscribe(x => System.Console.WriteLine($"!!--{i++}--!!"));
}

return WithMethod();