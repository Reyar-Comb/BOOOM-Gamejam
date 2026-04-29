using Godot;
using System;
using System.Diagnostics;
public class Benchmark
{
    public static void Run(Action action, int iterations = 1000)
    {
        if (action == null)
        {
            GD.PushError("Action cannot be null.");
            return;
        }
        if (iterations <= 0)
        {
            GD.PushError("Iterations must be greater than zero.");
            return;
        }
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            action();
        }
        watch.Stop();
        GD.Print($"Average execution time: {watch.Elapsed.TotalMilliseconds / iterations} ms");
    }
}
