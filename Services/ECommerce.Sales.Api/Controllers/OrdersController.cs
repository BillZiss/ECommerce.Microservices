﻿using System;
using System.Collections.Generic;
using System.Linq;
using ECommerce.Common;
using ECommerce.Common.Commands;
using ECommerce.Sales.Api.Model;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Sales.Api.Controllers
{
    [Route("api/[controller]")]
    public class OrdersController : Controller
    {
        private readonly IBus _bus;
        private readonly IConfiguration _cfg;

        public OrdersController(IBus bus, IConfiguration cfg)
        {
            _bus = bus;
            _cfg = cfg;
        }

        // GET api/orders
        [HttpGet]
        public IEnumerable<Order> Get()
        {
            using (SalesContext ctx = new SalesContext(_cfg["ConnectionString"]))
            {
                var orders = ctx.Orders.Include(o => o.Items).ToList();
                return orders;
            }
        }

        // POST api/orders
        [HttpPost]
        public async void Post([FromBody]SubmitOrder submittedOrder)
        {
            var command = new SubmitOrderCommand()
            {
                CustomerId = submittedOrder.CustomerId,
                Items = submittedOrder.Items.Select(t => new Item() { ProductId = t.ProductId, Quantity = t.Quantity }).ToArray()
            };

            var sendEndpoint = await _bus.GetSendEndpoint(new Uri($"rabbitmq://{Configuration.RabbitMqHost}/submit_orders"));
            await sendEndpoint.Send(command);
        }
    }
}
