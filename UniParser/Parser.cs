using System;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace UniParser
{
    public class ParsedThing
    {
        public string Title { get; set; }
        public int Price { get; set; }
        public string Url { get; set; }
    }
    public class MainParser
    {
        public async Task<ParsedThing> ParseThingAsync(string url)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            var page = await browser.NewPageAsync();

            await page.SetExtraHTTPHeadersAsync(new System.Collections.Generic.Dictionary<string, string>
        {
            {"User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36" }
        });
            try
            {
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                var titleElement = await page.QuerySelectorAsync("h1");
                string title = titleElement != null ? (await titleElement.InnerTextAsync()).Trim() : "Unknown product";

                var priceElement = await page.QuerySelectorAsync(".item__price-once");
                if (priceElement == null) return null;

                string rawPriceElement = await priceElement.InnerTextAsync();

                string PriceString = Regex.Replace(rawPriceElement, @"\D", "");

                int price = int.Parse(PriceString);

                return new ParsedThing
                {
                    Title = title,
                    Price = price,
                    Url = url
                };

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}][PARSER ERROR]: {ex.Message}");
                return null;
            }
        }
    }
}


