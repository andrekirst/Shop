using Microsoft.Extensions.Logging;
using ProductSearchService.API.Messaging;
using System;

namespace ProductSearchService.API.Events
{
    public class ProductsSearchedEvent : Event
    {
        public ProductsSearchedEvent(string filter, bool productsFound, int numberOfProductsFound)
        {
            Filter = filter;
            ProductsFound = productsFound;
            NumberOfProductsFound = numberOfProductsFound;
        }

        public ProductsSearchedEvent(Guid messageId, string filter, bool productsFound, int numberOfProductsFound)
            : this(filter: filter, productsFound: productsFound, numberOfProductsFound: numberOfProductsFound)
        {
            MessageId = messageId;
        }

        public string Filter { get; }

        public bool ProductsFound { get; }

        public int NumberOfProductsFound { get; }

        public override string ToString()
        {
            return $"Filter: {Filter}, ProductsFound: {ProductsFound}, NumberOfProductsFound: {NumberOfProductsFound}";
        }

        public override EventId EventId => new EventId(id: 300001, name: MessageType);
    }
}
