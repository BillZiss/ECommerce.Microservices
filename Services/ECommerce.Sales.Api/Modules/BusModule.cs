﻿using System;
using Autofac;
using ECommerce.Sales.Api.Consumers;
using ECommerce.Services.Common.Logging;
using MassTransit;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Sales.Api.Modules
{
    internal class BusModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context =>
            {
                var config = context.Resolve<IConfiguration>();
                var rabbitHost = config["RabbitHost"];
                var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    var host = cfg.Host(new Uri($"rabbitmq://{rabbitHost}"), h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    // https://stackoverflow.com/questions/39573721/disable-round-robin-pattern-and-use-fanout-on-masstransit
                    cfg.ReceiveEndpoint(host, "ecommerce_main_fanout" + Guid.NewGuid().ToString(), e =>
                    {
                        
                    });

                    cfg.ReceiveEndpoint(host, "sales_fanout", e =>
                    {
                        e.Consumer<OrderCompletedEventConsumer>(context);
                    });

                    cfg.ReceiveEndpoint(host, "sales_submit_orders", e =>
                    {
                        e.Consumer<SubmitOrderCommandConsumer>(context);
                    });
                });

                MassTransitAppender.Bus = busControl;

                return busControl;
            })
            .SingleInstance()
            .As<IPublishEndpoint>()
            .As<IBusControl>()
            .As<IBus>();
        }
    }
}
