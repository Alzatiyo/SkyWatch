using Microsoft.AspNetCore.SignalR;

namespace MS_ApiGateway.Hubs;

public class NotificationHub : Hub
{
    // Este método será llamado internamente o desde MS-EHRLogger
    // para enviar mensajes a todos los clientes web conectados
    public async Task SendEhrNotification(string message)
    {
        await Clients.All.SendAsync("ReceiveEhrNotification", message);
    }
}
