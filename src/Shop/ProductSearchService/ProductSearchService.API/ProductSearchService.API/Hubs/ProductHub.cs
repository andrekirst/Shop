using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ProductSearchService.API.Hubs
{
    public class ProductHub : Hub
    {
        public async Task SendMessage()
        {
            //await Clients.
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
