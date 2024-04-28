using QuantConnect;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Serialization;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.Composition;

namespace Panoptes.Model.Serialization
{
    public class OrderEventJsonConverter : TypeChangeJsonConverter<OrderEvent, SerializedOrderEvent>
    {
        private readonly string _algorithmId;

        /// <summary>
        /// True will populate TResult object returned by <see cref="Convert(SerializedOrderEvent)"/> with json properties
        /// </summary>
        protected override bool PopulateProperties => false;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="algorithmId">The associated algorithm id, required when serializing</param>
        public OrderEventJsonConverter(string algorithmId = null)
        {
            _algorithmId = algorithmId;
        }

        /// <summary>
        /// Convert the input value to a value to be serialzied
        /// </summary>
        /// <param name="value">The input value to be converted before serialziation</param>
        /// <returns>A new instance of TResult that is to be serialzied</returns>
        protected override SerializedOrderEvent Convert(OrderEvent value)
        {
            return new SerializedOrderEvent(value, _algorithmId);
        }

        /// <summary>
        /// Converts the input value to be deserialized
        /// </summary>
        /// <param name="value">The deserialized value that needs to be converted to <see cref="OrderEvent"/></param>
        /// <returns>The converted value</returns>
        protected override OrderEvent Convert(SerializedOrderEvent value)
        {
            return FromSerialized(value);
        }

        public static OrderEvent FromSerialized(SerializedOrderEvent serializedOrderEvent)
        {
            SecurityIdentifier securityIdentifier = SecurityIdentifier.Parse(serializedOrderEvent.Symbol);
            Symbol symbol = new Symbol(securityIdentifier, securityIdentifier.Symbol);
            OrderFee orderFee = OrderFee.Zero;
            if (serializedOrderEvent.OrderFeeAmount.HasValue)
            {
                orderFee = new OrderFee(new CashAmount(serializedOrderEvent.OrderFeeAmount.Value, serializedOrderEvent.OrderFeeCurrency));
            }

            return new OrderEvent(serializedOrderEvent.OrderId, symbol, DateTime.SpecifyKind(Time.UnixTimeStampToDateTime(serializedOrderEvent.Time), DateTimeKind.Utc), serializedOrderEvent.Status, serializedOrderEvent.Direction, serializedOrderEvent.FillPrice, serializedOrderEvent.FillQuantity, orderFee, serializedOrderEvent.Message)
            {
                IsAssignment = serializedOrderEvent.IsAssignment,
                IsInTheMoney = serializedOrderEvent.IsInTheMoney,
                LimitPrice = serializedOrderEvent.LimitPrice,
                StopPrice = serializedOrderEvent.StopPrice,
                FillPriceCurrency = serializedOrderEvent.FillPriceCurrency,
                Id = serializedOrderEvent.OrderEventId,
                Quantity = serializedOrderEvent.Quantity
            };
        }
    }
}
