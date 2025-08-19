namespace Credo.Core.Shared.Helpers;

public static class Helpers
{
    private static Dictionary<int, string> _currencies = new Dictionary<int, string>() { { 77, "GEL" }, { 78, "USD" }, { 10303, "EUR" }, { 33593, "RUB" }, { 33594, "GBP" } };

    public static int GetCurrencyId(this string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
        return _currencies.FirstOrDefault(x => x.Value == name).Key;
    }

    public static string GetCurrencyName(this int id)
    {
        if (id <= 0) throw new ArgumentNullException("id");
        return _currencies[id];
    }

    public static Dictionary<int, string> GetCurrenciesList() => _currencies;
}