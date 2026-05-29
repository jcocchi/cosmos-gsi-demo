using data_loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace query_gsi
{
    public class GSIOrder
    {
        public string Id { get; set; }
        public string _id { get; set; }
        public string CustomerId { get; set; }
        public Address ShippingAddress { get; set; }
    }
}
