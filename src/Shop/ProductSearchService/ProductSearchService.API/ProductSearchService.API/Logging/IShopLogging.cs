namespace ProductSearchService.API.Logging
{
    public interface IShopLogging
    {
        IShopAPILogging API { get; }

        IShopCommonLogging Common { get; }

        ShopLoggingOptions Options { get; }
    }
}
