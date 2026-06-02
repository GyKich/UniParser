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
            Console.WriteLine("[Система] Обнаружен режим миграций. Настройка конфигурации...");
            return; // Мгновенно выходим! Бот не запустится, файлы не заблокируются.
        }
        Console.WriteLine("=== ЗАПУСК СИСТЕМЫ ===");

        string keyPath = "key.txt";

        if (!File.Exists(keyPath))
        {
            Console.WriteLine($"[Критическая ошибка] Файл настроек '{keyPath}' не найден!");
            Console.WriteLine("Создай файл key.txt в папке с проектом и вставь туда токен от BotFather.");
            return;
        }
        string BotToken = (await File.ReadAllTextAsync(keyPath)).Trim();

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

        Console.WriteLine($"Bot started successfully");

        var priceWorker = new PriceMonitor(botClient);
        Task workerTask = priceWorker.StartAsync(cts.Token);

        Console.WriteLine("[SYSTEM] Press ENTER to stop");
        Console.ReadLine();

        cts.Cancel();

        await priceWorker.StopAsync(default);
    }
}
