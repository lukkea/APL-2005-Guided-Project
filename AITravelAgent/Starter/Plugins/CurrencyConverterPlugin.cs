using AITravelAgent.Plugins;
using Microsoft.SemanticKernel;
using System.ComponentModel;

internal class CurrencyConverterPlugin {
    private static readonly Dictionary<string, Currency> currencyDictionary = Currency.Currencies;

    [KernelFunction("ConvertAmount")]
    [Description("Converts an amount from one currency to another")]
    public static string ConvertAmount(string amount, string baseCurrencyCode, string targetCurrencyCode) {
        Currency targetCurrency = currencyDictionary[targetCurrencyCode];
        Currency baseCurrency = currencyDictionary[baseCurrencyCode];

        if (targetCurrency == null) {
            return targetCurrencyCode + " was not found";
        } else if (baseCurrency == null) {
            return baseCurrencyCode + " was not found";
        } else {
            double amountInUSD = double.Parse(amount) * baseCurrency.USDPerUnit;
            double result = amountInUSD * targetCurrency.UnitsPerUSD;
            return result + targetCurrencyCode;
        }
    }
}