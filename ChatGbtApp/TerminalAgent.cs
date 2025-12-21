using System.Diagnostics;

namespace ChatGbtApp;

public class TerminalAgent
{
    public static async Task StartAsync()
    {
        var chat = new OpenAiApi();


        Console.WriteLine();
        Console.WriteLine("Waiting for response...");

// Start elapsed seconds counter (readable helper)
        var (cts, elapsedTask, stopwatch) = StartElapsedCounter();

        var response = await chat.AskAsync(
            File.ReadAllText("../../../../Input/prompt.txt")
            + File.ReadAllText("../../../../Input/job.txt")
        );

// Stop counter and show final elapsed time
        cts.Cancel();
        stopwatch.Stop();
        try
        {
            await elapsedTask;
        }
        catch
        {
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine($"\nResponse received after {stopwatch.Elapsed.TotalSeconds:F0} s.");

        Console.WriteLine(response);

// Helper method for the elapsed counter (keeps main flow concise)
        static (CancellationTokenSource cts, Task task, Stopwatch stopwatch) StartElapsedCounter()
        {
            var cts = new CancellationTokenSource();
            var stopwatch = Stopwatch.StartNew();
            var task = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    Console.Write($"\rElapsed: {stopwatch.Elapsed.TotalSeconds:F0}s");
                    try
                    {
                        await Task.Delay(1000, cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // cancelled — exit loop gracefully
                    }
                }
            }, cts.Token);

            return (cts, task, stopwatch);
        }


        Console.WriteLine(response);
    }
}