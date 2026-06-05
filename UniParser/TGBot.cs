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
				text: "Wrong message: only \"/start\", \"/unsubscribe\" or links",
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
		if (command.StartsWith("/unsubscribe"))
		{
			string url = command.Replace("/unsubscribe ", "").Trim();

			if (string.IsNullOrEmpty(url) || !url.Contains("https://kaspi.kz"))
			{
				await _botClient.SendMessage(
					chatId: chatId,
					text: "Wrong Link, please enter a valid link after the command.",
					cancellationToken: cancellation
				);
				return;
			}

			await _botClient.SendMessage(chatId: chatId, text: "Processing your request", cancellationToken: cancellation);

			var subService = new SubscriptionService();
			var isRemoved = await subService.UnSubscribeUserAsync(chatId,url,cancellation);

			if (isRemoved)
			{
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Your subscription has been removed. You will no longer receive price updates for this item.",
                    cancellationToken: cancellation
                );
            }
			else
			{
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Subscription not found. Make sure you are subscribed to this exact link.",
                    cancellationToken: cancellation
                );
            }
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
		var subService = new SubscriptionService();
		var (isNew, product) = await subService.SubscribeUserAsync(chatId, parsedData, cancellationToken);

		if (isNew)
		{
            string responseText = $"Successfully added to your subscription list!\n" +
                              $"*{product.Title}*\n" +
                              $"Current price: {product.FixedPrice:N0} ₸\n\n" +
                              $"I'll send you notification when price change";

			await _botClient.SendMessage(
				chatId: chatId,
				text: responseText,
				parseMode: ParseMode.Markdown,
				cancellationToken: cancellationToken);

        }
		else
		{
            string responseText = $"This item is already in your subscription list!\n" +
                              $"*{product.Title}*\n" +
                              $"Current price: {product.FixedPrice:N0} ₸\n\n" +
                              $"I'll send you notification when price change";

            await _botClient.SendMessage(
                chatId: chatId,
                text: responseText,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
	}
	public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
	{
		Console.WriteLine($"[BOT] TELEGRAM API ERROR: {exception.Message}");
		return Task.CompletedTask;
	}
}
