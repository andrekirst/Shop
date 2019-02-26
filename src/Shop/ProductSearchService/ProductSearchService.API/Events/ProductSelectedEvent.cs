using Infrastructure.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductSearchService.API.Events
{
    public class ProductSelectedEvent : Event
    {
        public ProductSelectedEvent(Guid messageId, string productnumber, string name, string description)
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
