using System;
using Shop.Infrastructure.Messaging;

namespace ProductSearchService.API.Events
{
    public class ProductCreatedEvent : Event
    {
        public ProductCreatedEvent(Guid messageId, string productnumber, string name, string description)
            : base(messageId: messageId)
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
