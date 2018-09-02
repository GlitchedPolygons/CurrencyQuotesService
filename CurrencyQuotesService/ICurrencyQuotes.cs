using System.Threading.Tasks;

namespace GlitchedPolygons.Services.CurrencyQuotes
{
    /// <summary>
    /// Online currency quotes service for retrieving live exchange rates.<para> </para>
    /// Use this sparingly, as most free providers have hard limits on the amount of
    /// requests per month/day/whatever...
    /// </summary>
    public interface ICurrencyQuotes
    {
        /// <summary>
        /// Refreshes the current server's currency conversion quotes
        /// (retrieving fresh data from the exchange web api).<para> </para>
        /// Should return <c>true</c> if the refresh action was successful, and <c>false</c> if something went wrong.
        /// </summary>
        /// <returns>Whether the refresh action was successful or not.</returns>
        Task<bool> Refresh();

        /// <summary>
        /// Gets the specified currency conversion quote (with 1 USD as base).
        /// </summary>
        /// <param name="currency">The ISO name of the currency (e.g. CHF, USD, CZK, etc...).</param>
        /// <returns>The USD-to-currency quote if it could be found; <c>-1.0f</c> if no matching quote has been found.</returns>
        Task<float> GetConversionQuote(string currency);

        /// <summary>
        /// Converts the specified amount of USD (default is 1) into the target currency.
        /// </summary>
        /// <param name="currency">The ISO name of the currency.</param>
        /// <param name="amount">How many USD to convert.</param>
        /// <returns>The converted amount; <c>-1.0f</c> if the conversion failed in some way.</returns>
        Task<float> ConvertFromUSD(string currency, float amount = 1.0f);
    }
}
