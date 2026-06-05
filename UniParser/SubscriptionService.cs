using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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
                Console.WriteLine($"[{DateTime.Now}][DB] Added new product with ID: {productId}");
            }
            else
            {
                productId = existingProduct.Id;
                Console.WriteLine($"[{DateTime.Now}][DB] Product is already exist with ID: {productId}");

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

    public async Task<bool> UnSubscribeUserAsync(long chatId, string url, CancellationToken cancellationToken)
    {
        using (AppDbContext db = new AppDbContext())
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Url == url, cancellationToken);
            if (product == null)
            {
                return false;
            }

            var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.ChatId == chatId && s.TrackedProductId == product.Id, cancellationToken);
            if (subscription == null)
            {
                return false;
            }

            db.Subscriptions.Remove(subscription);
            await db.SaveChangesAsync(cancellationToken);

            bool hasOtherSubs = await db.Subscriptions.AnyAsync(s => s.TrackedProductId == product.Id, cancellationToken);
            if (!hasOtherSubs)
            {
                db.Products.Remove(product);
                await db.SaveChangesAsync(cancellationToken);
                Console.WriteLine($"[{DateTime.Now}][DB] Product {product.Id} removed from DB. Reason: 0 subscriptions.");
            }
            return true;
        }
    }

}
