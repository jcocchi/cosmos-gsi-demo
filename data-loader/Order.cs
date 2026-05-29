using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace data_loader
{
    public class Order
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string TenantId { get; set; }
        public List<Product> Products { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public PaymentInfo Payment { get; set; }
        public Address ShippingAddress { get; set; }
    }

    public class Product
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
    }

    public class PaymentInfo
    {
        public string TransactionId { get; set; }
        public bool Paid { get; set; }
    }
}
