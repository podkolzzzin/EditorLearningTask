using System.Diagnostics;

public class TimeMeasurement
{
    public IDisposable Measure(string description)
    {
        return new TimeMeasurementScope(description);
    }
    
    private class TimeMeasurementScope(string description) : IDisposable
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public void Dispose()
        {
            _stopwatch.Stop();
            Console.ResetColor();
            Console.WriteLine($">>>> {description}: {_stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }
    }
}