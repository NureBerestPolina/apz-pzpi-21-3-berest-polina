using System;

namespace EnRoute.API.Contracts.Orders.Requests
{
    public class MakeOrderRequest
    {
        public Guid PickupCounterId { get; set; }
        public Guid GoodId { get; set; }
        public Guid CustomerId { get; set; }
        public int AmountOrdered { get; set; }
    }
}
