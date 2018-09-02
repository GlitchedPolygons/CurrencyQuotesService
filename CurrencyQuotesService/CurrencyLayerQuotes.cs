using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using GlitchedPolygons.Services.Bash;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GlitchedPolygons.Services.CurrencyQuotes
{
    /// <summary>
    /// <see cref="ICurrencyQuotes"/> implementation that makes use of the CurrencyLayer.com API.<para> </para>
    /// Stores the exchange quotes inside a file. If no file r/w activity is wished, use the <see cref="CurrencyLayerQuotesSingleton"/> implementation!
    /// </summary>
    public class CurrencyLayerQuotes : ICurrencyQuotes
    {
        private const string FILE_PATH = "currencies.json";

        /// <summary>
        /// The default currency exchange data json.<para> </para>
        /// It's an example excerpt from the 7th of July 2018.
        /// </summary>
        public const string DEFAULT_JSON = @"{""timestamp"":""07/07/2018 01:00:00"",""source"":""USD"",""quotes"":{""USDCHF"":0.989304,""USDGBP"":0.75252,""USDCAD"":1.307904,""USDAUD"":1.345204,""USDCZK"":22.01804,""USDRUB"":62.925201}}";

        private readonly IBash bash;
        private readonly string url;
        private readonly int refreshRate;
        private readonly bool isLinux;

        private JObject json;

        /// <summary>
        /// Creates a new <see cref="CurrencyLayerQuotes"/> instance.
        /// Please ensure that the parameters passed into this constructor are valid!
        /// </summary>
        /// <param name="bash">Bash command service. (Use the injected service by calling provider.GetService)</param>
        /// <param name="currencyLayerApiKey">The currencylayer.com API key (REMINDER: please don't check any API keys into source control!).</param>
        /// <param name="refreshRate">Refresh the currency exchange quotes every {amount} minutes.</param>
        /// <param name="currencies">All ISO names of the currencies to query (e.g. CHF, EUR, CAD, ...).</param>
        public CurrencyLayerQuotes(IBash bash, string currencyLayerApiKey, int refreshRate, params string[] currencies)
        {
            if (currencies is null || currencies.Length == 0)
            {
                throw new ArgumentException($"{nameof(CurrencyLayerQuotes)}::ctor: The passed {nameof(currencies)} params array is either null or empty!");
            }

            this.bash = bash;
            this.refreshRate = Math.Abs(refreshRate);
            isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            //isLinux = Environment.OSVersion.Platform == PlatformID.Unix;

            if (isLinux && bash is null)
            {
                throw new ArgumentNullException($"{nameof(CurrencyLayerQuotes)}::ctor: The passed {nameof(bash)} parameter ({nameof(IBash)} instance) is null and would be needed since we're running on Linux here...");
            }

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

        /// <summary>
        /// Refreshes the current server's currency conversion quotes
        /// (retrieving fresh data from the exchange web api).<para> </para>
        /// Should return <c>true</c> if the refresh action was successful, and <c>false</c> if something went wrong.
        /// </summary>
        /// <returns>Whether the refresh action was successful or not.</returns>
        public async Task<bool> Refresh()
        {
            if (isLinux)
            {
                bash.Exec($"touch {FILE_PATH}");
                bash.Exec($"chmod 666 {FILE_PATH}");
            }

            if (!File.Exists(FILE_PATH))
            {
                File.WriteAllText(FILE_PATH, DEFAULT_JSON);
            }

            json = JObject.Parse(File.ReadAllText(FILE_PATH));

            if (!DateTime.TryParse(json["timestamp"].ToString(), out var timestamp))
            {
                throw new InvalidDataException($"{nameof(CurrencyLayerQuotes)}::{nameof(Refresh)}: Failure to parse {nameof(timestamp)} {nameof(DateTime)}. Please ensure valid JSON here!");
            }

            // Only request fresh json from the currency web API
            // if the minimum amount of time between refreshes has elapsed.
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

                File.WriteAllText(FILE_PATH, json.ToString(Formatting.Indented));
            }

            return true;
        }

        /// <summary>
        /// Gets the specified currency conversion quote (with 1 USD as base).
        /// </summary>
        /// <param name="currency">The ISO name of the currency.</param>
        /// <returns>The USD-to-currency quote if it could be found; <c>-1.0f</c> if no matching quote has been found.</returns>
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

        /// <summary>
        /// Converts the specified amount of USD (default is 1) into the target currency.<para> </para>
        /// Base currency is always USD due to the fact that the free plan of 
        /// CurrencyLayer API does not allow customizing the source currency for the requests.
        /// </summary>
        /// <param name="currency">The ISO name of the currency.</param>
        /// <param name="amount">How many USD to convert.</param>
        /// <returns>The converted amount; <c>-1.0f</c> if the conversion failed in some way.</returns>
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
