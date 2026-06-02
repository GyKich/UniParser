using System;
using Telegram.Bot.Types.Enums;

namespace UniParser;
public class SubscriptionService
{
	public async Task<(bool IsNew, TrackedProduct Product)> SubscribeUserAsync(long chatId, ParsedThing parsedData, CancellationToken cancellationToken)
	{
        using (AppDbContext db = new AppDbContext())
        {
            db.Database.EnsureCreated();

            var existingProduct = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                db.Products, p => p.Url == parsedData.Url, cancellationToken);

            int productId;

            if (existingProduct == null)
            {
                existingProduct = new TrackedProduct
                {
                    Title = parsedData.Title,
                    Url = parsedData.Url,
                    FixedPrice = parsedData.Price,
                    UpdatedAt = DateTime.Now
                };
                db.Products.Add(existingProduct);
                await db.SaveChangesAsync(cancellationToken);

                productId = existingProduct.Id;
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

                return (IsNew: true, Product: existingProduct);
            }
            return (IsNew: false, Product: existingProduct);
        }
    }

}
