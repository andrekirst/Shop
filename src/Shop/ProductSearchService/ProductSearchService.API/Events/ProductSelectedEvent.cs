using Microsoft.Extensions.Logging;
using ProductSearchService.API.Messaging;
using System;

namespace ProductSearchService.API.Events
{
    public class ProductSelectedEvent : Event
    {
        public ProductSelectedEvent(string productnumber, string name, string description)
            : base()
        {
            Productnumber = productnumber;
            Name = name;
            Description = description;
        }

        public ProductSelectedEvent(Guid messageId, string productnumber, string name, string description)
            : this(productnumber: productnumber, name: name, description: description)
        {
            MessageId = messageId;
        }

        public string Productnumber { get; }

        public string Name { get; }

        public string Description { get; }

        public override string ToString()
        {
            return $"Productnumber: {Productnumber}, Name: {Name ?? "<Empty>"}, Description: {Description ?? "<Empty>"}";
        }

        public override EventId EventId => new EventId(300000, MessageType);
    }
}
