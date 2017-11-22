using System;

namespace POSIntegrator
{
    public class TrnReceivingReceiptItem
    {
        public Int32 RRId { get; set; }
        public String ItemCode { get; set; }
        public String Item { get; set; }
        public String Particulars { get; set; }
        public String Unit { get; set; }
        public Decimal Quantity { get; set; }
        public Decimal Cost { get; set; }
        public Decimal Amount { get; set; }
        public String BaseUnit { get; set; }
        public Decimal BaseQuantity { get; set; }
        public Decimal BaseCost { get; set; }
    }
}
