using ProductSearchService.EventListener.Messaging;
using System;

namespace ProductSearchService.EventListener.Events
{
    public class ProductNameChangedEvent : Event
    {
        public ProductNameChangedEvent(Guid messageId, string productnumber, string name)
            : base(messageId: messageId)
        {
            Productnumber = productnumber;
            Name = name;
        }

        public string Productnumber { get; }
        
        public string Name { get; }
    }
}
