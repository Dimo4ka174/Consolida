using Application.DataAccessLayer.Interface.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Application.DataAccessLayer.Service.Hubs
{
    public class ConsolidationNotifier : IConsolidationNotifier
    {
        private readonly IHubContext<ConsolidationHub> _hubContext;

        public ConsolidationNotifier(IHubContext<ConsolidationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyBoardChangedAsync(ConsolidationChangeInfo info)
        {
            await _hubContext.Clients.All.SendAsync(ConsolidationHub.BoardChangedEvent, info);
        }
    }
}