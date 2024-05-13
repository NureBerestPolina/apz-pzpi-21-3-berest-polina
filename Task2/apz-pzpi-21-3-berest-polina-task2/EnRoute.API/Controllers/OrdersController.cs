using EnRoute.Domain.Models;
using EnRoute.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System;
using EnRoute.API.Contracts.Orders.Requests;
using EnRoute.Domain.Constants;
using EnRoute.Domain.Models.Interfaces;

namespace EnRoute.API.Controllers
{
    public class OrdersController : ODataControllerBase<Order>
    {
        private readonly IHttpClientFactory httpClientFactory;

        public OrdersController(ApplicationDbContext appDbContext, IHttpClientFactory httpClientFactory) : base(appDbContext)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async override Task<IActionResult> Put([FromRoute] Guid key, [FromBody] Order entity)
        {
            var order = AppDbContext.Orders.Include(o => o.AssignedCell).FirstOrDefault(o => o.Id == key);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = entity.Status;
            order.FinalizedDate = DateTime.UtcNow;

            var counter = AppDbContext.PickupCounters.FirstOrDefault(c => c.Id == order.AssignedCell.CounterId);

            //await UpdateDoorOpenCount(counter.URI);
            //await UpdateCounterStatistics(counter.URI);

            return await base.Put(key, order);
        }

        [HttpGet("counter/{counterId}/order-positions")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCounterOrderPositions([FromRoute] Guid counterId)
        {
            OrderItem[] result = await AppDbContext.OrderItems
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.AssignedCell)
                        .ThenInclude(c => c.Counter)
                .Include(oi => oi.GoodOrdered)
                    .ThenInclude(g => g.Category)
                .Where(oi => oi.Order.AssignedCell.CounterId == counterId)
                .ToArrayAsync();

            if (result == null || !result.Any())
            {
                return NotFound("No orders found for the specified counterId.");
            }

            return Ok(result);
        }

        private async Task UpdateDoorOpenCount(string uri)
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            var response = await client.PutAsJsonAsync(new Uri(uri + "/TechInspection/updateDoorOpenCount").ToString(), new { });
        }

        private async Task UpdateCounterStatistics(string uri)
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            var response = await client.PutAsJsonAsync(new Uri(uri + "/Statistics/updateStatistics").ToString(), new { });
        }

        [HttpPost]
        [Route("MakeOrder")]
        public async Task<IActionResult> MakeOrder([FromBody] MakeOrderRequest request)
        {
            var order = new Order
            {
                CustomerId = request.CustomerId
            };

            var goodOrdered = await AppDbContext.Goods.FindAsync(request.GoodId);
            var orderNeedsCooling = goodOrdered.NeedsCooling;
            var orderItem = new OrderItem
            {
                Count = request.AmountOrdered,
                GoodId = request.GoodId,
                OrderId = order.Id
            };

            var assignedCell = await AppDbContext.Cells
                .FirstAsync(c => c.hasTemperatureControl == orderNeedsCooling
                    && c.IsFree
                    && c.CounterId == request.PickupCounterId);

            assignedCell.IsFree = false;
            order.AssignedCellId = assignedCell.Id;
            order.Items.Add(orderItem);
            order.AssignedCell = default;

            AppDbContext.Orders.Add(order);

            await AppDbContext.SaveChangesAsync();


            return Ok(order);
        }
    }
}
