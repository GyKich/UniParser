using System;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace UniParser;
public class PriceMonitor : BackgroundService
{
	private readonly ITelegramBotClient _botClient;

	private readonly TimeSpan _checkInterval = TimeSpan.FromHours(2);
	public PriceMonitor(ITelegramBotClient botClient)
	{
		_botClient = botClient;
	}

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Console.WriteLine($"[{DateTime.Now}][WORKER] Background monitoring started succesfully...");

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				Console.WriteLine($"[{DateTime.Now}][WORKER] Starting database check...");
				await CheckPricesAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[{DateTime.Now}][WORKER ERROR]: {ex.Message}");
			}
			await Task.Delay(_checkInterval, stoppingToken);
		}
	}
	private async Task CheckPricesAsync(CancellationToken cancellationToken)
	{
		using var db = new AppDbContext();
		var products = await db.Products.ToListAsync(cancellationToken);

		if (!products.Any())
		{
			Console.WriteLine($"[{DateTime.Now}][WORKER] No products in the database.");
			return;
		}

		MainParser parser = new MainParser();
		
		foreach(var product in products)
		{
			if (cancellationToken.IsCancellationRequested) break;

			Console.WriteLine($"[{DateTime.Now}][WORKER Checking product: {product.Title}]");

			var parsedData = await parser.ParseThingAsync(product.Url);

			if (parsedData == null)
			{
				Console.WriteLine($"[{DateTime.Now}][WORKER] Product {product.Id} parsed unsuccesfuly, skipping...");
				continue;
			}

			if (parsedData.Price != product.FixedPrice)
			{
				int oldPrice = product.FixedPrice;
				int newPrice = parsedData.Price;

				Console.WriteLine($"[{DateTime.Now}][WORKER] Price for {product.Title} changed: {oldPrice} -> {newPrice}");
				product.FixedPrice = newPrice;
				product.UpdatedAt = DateTime.Now;

				var subscribers = await db.Subscriptions
					.Where(s => s.TrackedProductId == product.Id)
					.ToListAsync(cancellationToken);

				foreach (var sub in subscribers)
				{
					try
					{
						string direcion = newPrice < oldPrice ? "Price falled" : "Price rised";
						string messageText = $"{direcion}\n\n" + $"{product.Title}" + $"Old price: {oldPrice:N0} \n" + $"New price: {newPrice:N0} \n\n" + $"Product link: {product.Url}";

						await _botClient.SendMessage(
							chatId: sub.ChatId,
							text: messageText,
							parseMode: ParseMode.Markdown,
							cancellationToken: cancellationToken
						);
					}
					catch(Exception ex)
					{
						Console.WriteLine($"[{DateTime.Now}][WORKER] Unsuccessfully message sending to {sub.ChatId}: {ex.Message}");
					}
				}
			}
			else
			{
				Console.WriteLine($"[{DateTime.Now}][WORKER] No changes: {product.FixedPrice}");
			}

			Console.WriteLine($"[{DateTime.Now}][WORKER] Waiting crawl-delay...");
			await Task.Delay(10000, cancellationToken);
		}

		await db.SaveChangesAsync(cancellationToken);
		Console.WriteLine($"[{DateTime.Now}][WORKER] Whole database checked, changes saved.");
	}
}
