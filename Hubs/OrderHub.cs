using Microsoft.AspNetCore.SignalR;

namespace webHuyBeo.Hubs
{
    public class OrderHub : Hub
    {
        // Hub này được dùng để gửi tín hiệu Real-time giữa các client (Khách -> Bếp -> Thu ngân)
        // Các phương thức có thể được gọi từ Client hoặc từ Server thông qua IHubContext
        
        public async Task NotifyNewOrder()
        {
            // Báo cho toàn bộ client biết có đơn mới (Thường gửi đến Bếp)
            await Clients.All.SendAsync("ReceiveNewOrder");
        }

        public async Task NotifyOrderStatusChanged(int donHangId, string trangThai)
        {
            // Báo cho Thu ngân/Khách biết trạng thái đơn thay đổi
            await Clients.All.SendAsync("ReceiveOrderStatusChanged", donHangId, trangThai);
        }
    }
}
