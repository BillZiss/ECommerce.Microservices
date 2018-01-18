﻿using System;
using Autofac;
using ECommerce.Shipping.Host.Consumers;
using MassTransit;

namespace ECommerce.Shipping.Host.Modules
{
    internal class BusModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context =>
            {
                var rabbitHost = "localhost";
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
                        e.LoadStateMachineSagas(context);
                    });

                    cfg.ReceiveEndpoint(host, "shiporder", e =>
                    {
                        e.Consumer<ShipOrderCommandConsumer>(context);
                    });

                    cfg.ReceiveEndpoint(host, "packorder", e =>
                    {
                        e.Consumer<InitiateOrderPackingCommandConsumer>(context);
                    });
                });

                return busControl;
            })
            .SingleInstance()
            .As<IPublishEndpoint>()
            .As<IBusControl>()
            .As<IBus>();
        }
    }
}
