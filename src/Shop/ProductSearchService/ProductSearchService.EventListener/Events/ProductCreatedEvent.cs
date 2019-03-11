using ProductSearchService.EventListener.Messaging;
using System;

namespace ProductSearchService.EventListener.Events
{
    public class ProductCreatedEvent : Event
    {
        public ProductCreatedEvent(Guid messageId, string productnumber, string name, string description)
            : base(messageId)
        {
            Productnumber = productnumber;
            Name = name;
            Description = description;
        }

        public string Productnumber { get; }

        public string Name { get; }

        public string Description { get; }
    }
}
