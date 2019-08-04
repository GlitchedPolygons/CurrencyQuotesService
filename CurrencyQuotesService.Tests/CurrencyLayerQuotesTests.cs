using System;
using Xunit;

namespace GlitchedPolygons.Services.CurrencyQuotes.Tests
{
    public class CurrencyLayerQuotesTests
    {
        [Theory]
        [InlineData("api_key", 20, null)]
        [InlineData("api_key", 20, new string[0])]
        [InlineData("", -20, new[] { "chf", "usd", "czk" })]
        [InlineData(null, -20, new[] { "chf", "usd", "czk" })]
        public void Ctor_TestInvalidParams_ThrowsExceptions(string apiKey, int refreshRate, string[] currencies)
        {
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                new CurrencyLayerQuotes(new Bash.Bash(), apiKey, refreshRate, currencies);
            });
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("INVALID_CURRENCY_ISO")]
        public void GetConversionQuote_InvalidCurrencyISO_ReturnsMinusOne(string currency)
        {
            var quotes = new CurrencyLayerQuotes(new Bash.Bash(), "api_key", 20, "chf", "usd", "czk");
            var quote = quotes.GetConversionQuote(currency).Result;
            Assert.True(quote < 0);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("INVALID_CURRENCY_ISO")]
        public void ConvertFromUSD_InvalidCurrencyISO_ReturnsMinusOne(string currency)
        {
            var quotes = new CurrencyLayerQuotes(new Bash.Bash(), "api_key", 20, "chf", "usd", "czk");
            var convertedAmount = quotes.ConvertFromUSD(currency);
            Assert.True(convertedAmount.Result < 0);
        }
    }
}
