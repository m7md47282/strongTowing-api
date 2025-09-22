using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace towing_services.Hubs
{
    public class OrderHub : Hub<IOrderHub> // استخدام IOrderHub هنا
    {

        public async Task SendNewOrderCount(int newOrderCount)
        {
            // إرسال الإشعار لجميع المتصلين عبر الـ Hub
            await Clients.All.ReceiveNewOrderNotification(newOrderCount);
        }
        // إضافة طريقة Ping لضمان عملها مع العميل
        public async Task Ping()
        {
            // يمكن هنا إضافة أي منطق تريد تنفيذه عند تلقي Ping
            await Task.CompletedTask; // هذه هي طريقة فارغة ترد فقط لتأكيد الاتصال
        }

    }
    public interface IOrderHub
    {
        Task ReceiveNewOrderNotification(int newOrderCount);
    }


    
}

