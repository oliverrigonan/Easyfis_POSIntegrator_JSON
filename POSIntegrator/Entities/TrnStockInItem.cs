using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIntegrator
{
    class TrnStockInItem
    {
        public Int32 INId { get; set; }
        public String ItemCode { get; set; }
        public String Item { get; set; }
        public String Unit { get; set; }
        public Decimal Quantity { get; set; }
        public Decimal Cost { get; set; }
        public Decimal Amount { get; set; }
    }
}
