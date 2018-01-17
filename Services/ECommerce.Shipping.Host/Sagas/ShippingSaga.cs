﻿using System;
using Automatonymous;
using ECommerce.Common.Commands;
using ECommerce.Common.Events;

namespace ECommerce.Shipping.Host.Sagas
{
    public class ShippingSaga : MassTransitStateMachine<Shipment>
    {
        public ShippingSaga()
        {
            InstanceState(t => t.CurrentState);

            Event(() => OrderSubmitted, x => x.CorrelateById(s => s.OrderId, context => context.Message.OrderId).SelectId(context => Guid.NewGuid()));

            Event(() => PaymentReceived, x => x.CorrelateById(s => s.OrderId, context => context.Message.OrderId));

            Event(() => OrderPacked, x => x.CorrelateById(s => s.OrderId, context => context.Message.OrderId));

            Event(() => OrderCompleted, x => x.CorrelateById(s => s.OrderId, context => context.Message.OrderId));

            Initially(
                When(OrderSubmitted)
                .Then(OnOrderSubmitted)
                .TransitionTo(Submitted));

            During(Submitted,
                   When(PaymentReceived).Then(OnPaymentReceived),
                   When(OrderPacked).Then(OnOrderPacked),
                   When(OrderCompleted).Then(OnOrderComplete).TransitionTo(Shipped).Finalize()
                  );

            SetCompletedWhenFinalized();
        }

        private async void OnOrderSubmitted(BehaviorContext<Shipment, OrderSubmittedEvent> context)
        {
            context.Instance.CustomerId = context.Data.CustomerId;
            context.Instance.OrderId = context.Data.OrderId;

            Console.WriteLine($"Saga: Order {context.Instance.OrderId} submitted by customer {context.Instance.CustomerId}");

            var endpoint = await context.GetSendEndpoint(new Uri("rabbitmq://localhost/packorder"));
            await endpoint.Send(new InitiateOrderPackingCommand() { CustomerId = context.Instance.CustomerId, OrderId = context.Instance.OrderId });
        }

        private async void OnOrderPacked(BehaviorContext<Shipment, OrderPackedEvent> context)
        {
            context.Instance.IsPacked = true;
            Console.WriteLine($"Saga: Order {context.Instance.OrderId} submitted by customer {context.Instance.CustomerId} is packed.");

            if (context.Instance.IsPacked && context.Instance.IsPayed)
            {
                var endpoint = await context.GetSendEndpoint(new Uri("rabbitmq://localhost/shiporder"));
                await endpoint.Send(new ShipOrderCommand() { CustomerId = context.Instance.CustomerId, OrderId = context.Instance.OrderId });
            }
        }

        private async void OnPaymentReceived(BehaviorContext<Shipment, PaymentAcceptedEvent> context)
        {
            context.Instance.IsPayed = true;
            Console.WriteLine($"Saga: Order {context.Instance.OrderId} submitted by customer {context.Instance.CustomerId} is payed.");

            if (context.Instance.IsPacked && context.Instance.IsPayed)
            {
                var endpoint = await context.GetSendEndpoint(new Uri("rabbitmq://localhost/shiporder"));
                await endpoint.Send(new ShipOrderCommand() { CustomerId = context.Instance.CustomerId, OrderId = context.Instance.OrderId });
            }
        }

        private async void OnOrderComplete(BehaviorContext<Shipment, OrderCompletedEvent> context)
        {
            Console.WriteLine($"Saga: Order {context.Instance.OrderId} submitted by customer {context.Instance.CustomerId} has shipped.");
        }

        public State Submitted { get; set; }

        public State Shipped { get; set; }

        public Event<OrderSubmittedEvent> OrderSubmitted { get; private set; }

        public Event<PaymentAcceptedEvent> PaymentReceived { get; private set; }

        public Event<OrderPackedEvent> OrderPacked { get; private set; }

        public Event<OrderCompletedEvent> OrderCompleted { get; private set; }
    }
}
