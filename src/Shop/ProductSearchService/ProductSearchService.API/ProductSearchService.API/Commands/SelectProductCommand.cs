using ProductSearchService.API.Messaging;
using System;

namespace ProductSearchService.API.Commands
{
    public class SelectProductCommand : Command
    {
        public SelectProductCommand(Guid messageId, string productnumber, string name, string description)
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
