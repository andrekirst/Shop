namespace ProductSearchService.API.Logging
{
    public interface IShopLogging
    {
        IShopApiLogging Api { get; }

        IShopCommonLogging Common { get; }

        ShopLoggingOptions Options { get; }
    }
}
