using System;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using System.Linq;

namespace UniParser;
public class Program
{
    static async Task Main(string[] args)
    {
        bool isMigrationMode = AppDomain.CurrentDomain.FriendlyName.Contains("ef") ||
                           AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name!.Contains("Design"));

        if (isMigrationMode)
        {
            Console.WriteLine($"[{DateTime.Now}][SYSTEM] MIGRATION MODE. Setting up configuration...");
            return;
        }
        Console.WriteLine($"[{DateTime.Now}][SYSTEM] START UP");

        using (var db = new AppDbContext())
        {
            db.Database.EnsureCreated();
        }

        string BotToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");

        if (string.IsNullOrEmpty(BotToken))
        {
            string keyPath = "key.txt";

            if (!File.Exists(keyPath))
            {
                Console.WriteLine($"[{DateTime.Now}][ERROR] Key file '{keyPath}' not found!");
                Console.WriteLine("Create key.txt file in the project folder and insert Telegram bot key in it");
                return;
            }
            BotToken = (await File.ReadAllTextAsync(keyPath)).Trim();
        }
        

        var botClient = new TelegramBotClient(BotToken);

        var botHandler = new TelegramBotHandler(botClient);

        using var cts = new CancellationTokenSource();

        botClient.StartReceiving(
            updateHandler: botHandler.HandleUpdateAsync,
            errorHandler: botHandler.HandleErrorAsync,
            receiverOptions: new() { AllowedUpdates = Array.Empty<UpdateType>()},
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMe(cancellationToken: cts.Token);

        Console.WriteLine($"[{DateTime.Now}][SYSTEM] Bot started successfully");

        var priceWorker = new PriceMonitor(botClient);
        Task workerTask = priceWorker.StartAsync(cts.Token);

        Console.WriteLine("[SYSTEM] Press ENTER to stop");
        await Task.Delay(Timeout.Infinite);

        cts.Cancel();

        await priceWorker.StopAsync(default);
    }
}
