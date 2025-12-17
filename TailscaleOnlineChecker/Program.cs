
using System;
using System.Threading.Tasks;

namespace TailscaleOnlineChecker
{

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tailscale Online Checker Starting...");

            // 从环境变量读取检查间隔（单位：分钟）
            var checkIntervalMinutes = GetCheckInterval();
            Console.WriteLine($"Check interval: {checkIntervalMinutes} minutes");

            while (true)
            {
                try
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting check...");
                    var worker = new TailscaleWorker();
                    await worker.HandleRequestAsync();
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Check completed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error occurred: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }

                // 等待指定的时间间隔
                var delayMilliseconds = checkIntervalMinutes * 60 * 1000;
                Console.WriteLine($"Waiting {checkIntervalMinutes} minutes until next check...");
                await Task.Delay(delayMilliseconds);
            }
        }

        private static int GetCheckInterval()
        {
            var intervalStr = Environment.GetEnvironmentVariable("CHECK_INTERVAL_MINUTES");

            if (string.IsNullOrEmpty(intervalStr))
            {
                Console.WriteLine("CHECK_INTERVAL_MINUTES not set, using default: 5 minutes");
                return 5; // 默认5分钟
            }

            if (int.TryParse(intervalStr, out int interval) && interval > 0)
            {
                return interval;
            }

            Console.WriteLine($"Invalid CHECK_INTERVAL_MINUTES value: {intervalStr}, using default: 5 minutes");
            return 5;
        }
    }
}


