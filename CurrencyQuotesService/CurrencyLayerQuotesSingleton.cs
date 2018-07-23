using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace GlitchedPolygons.Services.CurrencyQuotes
{
    /// <summary>
    /// <see cref="ICurrencyQuotes"/> singleton where service lifetime == app lifetime
    /// that makes use of the CurrencyLayer.com API.<para> </para>
    /// Keeps the currency exchange quotes in memory instead of in a file...
    /// </summary>
    public class CurrencyLayerQuotesSingleton : ICurrencyQuotes
    {
        private readonly string url;
        private readonly int refreshRate;

        private JObject json;

        /// <summary>
        /// Creates a new <see cref="CurrencyLayerQuotes"/> singleton instance (keeps the currencies in memory instead of in a file).
        /// Please ensure that the parameters passed into this constructor are valid!
        /// </summary>
        /// <param name="currencyLayerApiKey">The currencylayer.com API key (REMINDER: please don't check any API keys into source control!).</param>
        /// <param name="refreshRate">Refresh the currency exchange quotes every {amount} minutes.</param>
        /// <param name="currencies">All ISO names of the currencies to query (e.g. CHF, EUR, CAD, ...).</param>
        public CurrencyLayerQuotesSingleton(string currencyLayerApiKey, int refreshRate, params string[] currencies)
        {
            if (currencies is null || currencies.Length == 0)
            {
                throw new ArgumentException($"{nameof(CurrencyLayerQuotes)}::ctor: The passed {nameof(currencies)} params array is either null or empty!");
            }

            this.refreshRate = Math.Abs(refreshRate);
            json = JObject.Parse(CurrencyLayerQuotes.DEFAULT_JSON);

            string _currencies = currencies[0];
            if (currencies.Length > 1)
            {
                _currencies += ',';
                for (int i = 1; i < currencies.Length; i++)
                {
                    _currencies += currencies[i];
                    if (i < currencies.Length - 1)
                    {
                        _currencies += ',';
                    }
                }
            }

            url = $"http://apilayer.net/api/live?access_key={currencyLayerApiKey}&currencies={_currencies.ToUpper()}&source=USD&format=1";
        }

        public async Task<bool> Refresh()
        {
            if (!DateTime.TryParse(json["timestamp"].ToString(), out var timestamp))
            {
                throw new InvalidDataException($"{nameof(CurrencyLayerQuotes)}::{nameof(Refresh)}: Failure to parse {nameof(timestamp)} {nameof(DateTime)}. Please ensure valid JSON here!");
            }

            // Only request fresh json from the currency web API
            // if the minimum amount of time between refreshs has elapsed.
            if ((DateTime.Now - timestamp).TotalMinutes <= refreshRate)
            {
                return true;
            }

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                json = JObject.Parse(await response.Content.ReadAsStringAsync());
                json["timestamp"].Replace(DateTime.Now.ToString("O"));
            }

            return true;
        }

        public async Task<float> GetConversionQuote(string currency)
        {
            if (string.IsNullOrEmpty(currency) || !await Refresh())
            {
                return -1.0f;
            }

            if (json is null)
            {
                throw new InvalidDataException($"{nameof(CurrencyLayerQuotes)}::{nameof(GetConversionQuote)}: the currency quotes {nameof(json)} object is null! Please ensure there is at least the default json available for currency conversions.");
            }

            if (!json.HasValues)
            {
                throw new InvalidDataException($"{nameof(CurrencyLayerQuotes)}::{nameof(GetConversionQuote)}: Invalid JSON! Seems to have no values...");
            }

            JToken quotes = json["quotes"];

            if (quotes is null || !quotes.HasValues)
            {
                throw new InvalidDataException($"{nameof(CurrencyLayerQuotes)}::{nameof(GetConversionQuote)}: Invalid JSON! The {nameof(quotes)} element seems to have no values nested in it...");
            }

            float quote = quotes.Value<float>($"USD{currency.ToUpper()}");

            if (quote <= float.Epsilon)
            {
                throw new InvalidDataException($"{nameof(CurrencyLayerQuotes)}::{nameof(GetConversionQuote)}: The specified currency exchange quote 'USD{currency.ToUpper()}' does not exist!");
            }

            return quote;
        }

        public async Task<float> ConvertFromUSD(string currency, float amount = 1.0f)
        {
            if (string.IsNullOrEmpty(currency))
            {
                return -1.0f;
            }

            if (amount < 0.0f)
            {
                amount *= -1.0f;
            }

            float quote = await GetConversionQuote(currency);
            if (quote < 0.0f)
            {
                return -1.0f;
            }

            return quote * amount;
        }
    }
}

// Copyright (C) - Raphael Beck, 2018
