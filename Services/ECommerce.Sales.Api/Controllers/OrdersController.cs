﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Common.Commands;
using ECommerce.Sales.Api.Model;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Sales.Api.Controllers
{
    [Route("api/[controller]")]
    public class OrdersController : Controller
    {
        private readonly IBus _bus;

        public OrdersController(IBus bus)
        {
            this._bus = bus;
        }

        // GET api/orders
        [HttpGet]
        public IEnumerable<Order> Get()
        {
            using (SalesContext ctx = new SalesContext())
            {
                var orders = ctx.Orders.Include(o => o.Items).ToList();
                return orders;
            }
        }

        // POST api/orders
        [HttpPost]
        public void Post([FromBody]SubmitOrder submittedOrder)
        {
            var command = new SubmitOrderCommand()
            {
                CustomerId = submittedOrder.CustomerId,
                Items = submittedOrder.Items.Select(t => new Item() { ProductId = t.ProductId, Quantity = t.Quantity }).ToArray()
            };

            _bus.Publish(command);
        }
    }
}
