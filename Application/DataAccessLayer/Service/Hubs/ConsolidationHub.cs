using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Application.DataAccessLayer.Service.Hubs
{
    [Authorize(Policy = "ReadOrders")]
    public class ConsolidationHub : Hub
    {
        public const string BoardChangedEvent = "ConsolidationBoardChanged";
        public const string OrderDragStartedEvent = "OrderDragStarted";
        public const string OrderDragEndedEvent = "OrderDragEnded";
        public const string ViewersChangedEvent = "ViewersChanged";

        private static readonly ConcurrentDictionary<string, Viewer> _viewers = new();

        private class Viewer
        {
            public string UserId { get; set; } = string.Empty;
            public DateTime ConnectedAt { get; set; }
        }

        public override async Task OnConnectedAsync()
        {
            _viewers[Context.ConnectionId] = new Viewer
            {
                UserId = GetDisplayName(),
                ConnectedAt = DateTime.UtcNow
            };

            await base.OnConnectedAsync();
            await BroadcastViewersAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _viewers.TryRemove(Context.ConnectionId, out _);
            await BroadcastViewersAsync();
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Клиент вызывает сразу после подключения, чтобы получить список зрителей.
        /// </summary>
        public Task RequestViewers()
        {
            return Clients.Caller.SendAsync(ViewersChangedEvent, GetUniqueViewers());
        }

        public async Task BroadcastOrderDragStart(int orderId)
        {
            await Clients.Others.SendAsync(OrderDragStartedEvent, new
            {
                OrderId = orderId,
                UserId = GetDisplayName(),
                StartedAt = DateTime.UtcNow
            });
        }

        public async Task BroadcastOrderDragEnd(int orderId)
        {
            await Clients.Others.SendAsync(OrderDragEndedEvent, new { OrderId = orderId });
        }

        private async Task BroadcastViewersAsync()
        {
            await Clients.All.SendAsync(ViewersChangedEvent, GetUniqueViewers());
        }

        /// <summary>
        /// Один пользователь = одна запись, даже если открыл много вкладок.
        /// </summary>
        private List<ViewerInfo> GetUniqueViewers()
        {
            return _viewers.Values
                .GroupBy(v => v.UserId)
                .Select(g => new ViewerInfo
                {
                    UserId = g.Key,
                    ConnectedAt = g.Min(v => v.ConnectedAt)
                })
                .OrderBy(v => v.UserId)
                .ToList();
        }

        private string GetDisplayName()
        {
            return Context.User?.FindFirst("DisplayName")?.Value
                ?? Context.User?.Identity?.Name
                ?? "Unknown";
        }

        public class ViewerInfo
        {
            public string UserId { get; set; } = string.Empty;
            public DateTime ConnectedAt { get; set; }
        }
    }
}