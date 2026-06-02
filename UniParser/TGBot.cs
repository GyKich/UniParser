using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;



namespace UniParser;
public class TelegramBotHandler
{
	private readonly ITelegramBotClient _botClient;

	public TelegramBotHandler(ITelegramBotClient botClient)
	{
		_botClient = botClient;
	}

	public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
	{
		if (update.Type != UpdateType.Message || update.Message?.Text == null) return;

		var message = update.Message;
		var chatId = message.Chat.Id;
		var messageText = message.Text.Trim();

		Console.WriteLine($"[BOT] Message from {chatId}: \"{messageText}\"");

		if (messageText.StartsWith("/"))
		{
			await HandleCommandAsync(chatId, messageText, cancellationToken);
		}
		else if (messageText.StartsWith("https://kaspi.kz"))
		{
			await HandleLinkAsync(chatId, messageText, cancellationToken);
		}
		else
		{
			await _botClient.SendMessage(
				chatId: chatId,
				text:"Wrong message: only \"/start\" or links",
				cancellationToken: cancellationToken
			);
		}
	}
	private async Task HandleCommandAsync(long chatId, string command, CancellationToken cancellation)
	{
		if (command == "/start")
		{
			await _botClient.SendMessage(
				chatId: chatId,
				text: "Good day. Send me product link from Kaspy to subscribe on it",
				cancellationToken: cancellation
			);
		}
	}
	private async Task HandleLinkAsync(long chatId, string url, CancellationToken cancellationToken)
	{
		await _botClient.SendMessage(chatId, "Checking your page...", cancellationToken: cancellationToken);
		MainParser parser = new MainParser();
		var parsedData = await parser.ParseThingAsync(url);

		if(parsedData == null)
		{
			await _botClient.SendMessage(chatId: chatId, text: "Wrong link", cancellationToken: cancellationToken);
			return;
		}
		using (AppDbContext db = new AppDbContext())
		{
			db.Database.EnsureCreated();

			var existingProduct = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
				db.Products, p => p.Url == url, cancellationToken);

			int productId;

			if(existingProduct == null)
			{
                var newProduct = new TrackedProduct
                {
                    Title = parsedData.Title,
                    Url = parsedData.Url,
                    FixedPrice = parsedData.Price,
                    UpdatedAt = DateTime.Now
                };
                db.Products.Add(newProduct);
                await db.SaveChangesAsync(cancellationToken);

                productId = newProduct.Id;
                Console.WriteLine($"[DB] Added new product with ID: {productId}");
            }
            else
            {
                productId = existingProduct.Id;
                Console.WriteLine($"[DB] Product is already exist with ID: {productId}");

                existingProduct.FixedPrice = parsedData.Price;
                existingProduct.UpdatedAt = DateTime.Now;
                await db.SaveChangesAsync(cancellationToken);
            }
            var existingSub = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    db.Subscriptions, s => s.ChatId == chatId && s.TrackedProductId == productId, cancellationToken);

            if (existingSub == null)
            {
                var newSubscription = new UserMonitored
                {
                    ChatId = chatId,
                    TrackedProductId = productId
                };
                db.Subscriptions.Add(newSubscription);
                await db.SaveChangesAsync(cancellationToken);

                string responseText = $"✅ Товар успешно добавлен в твой список мониторинга!\n\n" +
                                      $"📦 *{parsedData.Title}*\n" +
                                      $"💰 Текущая цена: {parsedData.Price:N0} ₸\n\n" +
                                      $"Я пришлю уведомление, если цена изменится.";

                await _botClient.SendMessage(
                    chatId: chatId,
                    text: responseText,
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: $"⚠️ Товар уже зафиксирован!\n\n📦 *{parsedData.Title}*\n💰 Цена в базе: {parsedData.Price:N0} ₸",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
            }
        }
	}
	public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
	{
		Console.WriteLine($"[BOT] TELEGRAM API ERROR: {exception.Message}");
		return Task.CompletedTask;
	}
}
