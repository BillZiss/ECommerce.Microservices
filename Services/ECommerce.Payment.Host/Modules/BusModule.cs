﻿using Autofac;
using ECommerce.Common.Commands;
using ECommerce.Common.Infrastructure.Messaging;
using ECommerce.Payment.Host.Consumers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using System;

namespace ECommerce.Payment.Host.Modules
{
    internal class BusModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MessageCorrelationContextAccessor>().SingleInstance().As<IMessageCorrelationContextAccessor>();
            builder.Register(context =>
            {
                var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    var correlationContextAccessor = context.Resolve<IMessageCorrelationContextAccessor>();
                    var config = context.Resolve<IConfiguration>();
                    var rabbitHost = config["Brokers:RabbitMQ:Host"];
                    var username = config["Brokers:RabbitMQ:Username"];
                    var password = config["Brokers:RabbitMQ:Password"];

                    var host = cfg.Host(new Uri($"rabbitmq://{rabbitHost}"), h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });

                    cfg.UseCorrelationId(correlationContextAccessor);

                    cfg.ReceiveEndpoint(host, "payment_fanout", e =>
                    {
                        e.Consumer<OrderSubmittedEventConsumer>(context);
                    });

                    cfg.ReceiveEndpoint(host, "payment_initiate_payment", e =>
                    {
                        e.Consumer<InitiatePaymentCommandConsumer>(context);
                        
                        EndpointConvention.Map<InitiatePaymentCommand>(e.InputAddress);
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
