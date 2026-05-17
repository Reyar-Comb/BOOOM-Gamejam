using Godot;
using System;
public class Benchmark
{
    public static void Run(Action action, int iterations = 1000)
    {
        if (action == null)
        {
            Debug.PushError("Action cannot be null.");
            return;
        }
        if (iterations <= 0)
        {
            Debug.PushError("Iterations must be greater than zero.");
            return;
        }
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            action();
        }
        watch.Stop();
        Debug.Print($"Average execution time: {watch.Elapsed.TotalMilliseconds / iterations} ms");
    }
}

