using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ProductSearchService.EventListener.SignalR
{
    public class ProductNameChangedHub : Hub
    {
        public async Task SendMessage(string productnumber)
        {
            //await Clients.Caller.SendAsync()
        }
    }
}
