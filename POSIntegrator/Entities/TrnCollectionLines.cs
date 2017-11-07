using System;

namespace POSIntegrator
{
    public class TrnCollectionLines
    {
        public String ItemManualArticleCode { get; set; }
        public String Particulars { get; set; }
        public String Unit { get; set; }
        public Decimal Quantity { get; set; }
        public Decimal Price { get; set; }
        public String Discount { get; set; }
        public Decimal DiscountAmount { get; set; }
        public Decimal NetPrice { get; set; }
        public Decimal Amount { get; set; }
        public String VAT { get; set; }
        public String SalesItemTimeStamp { get; set; }
    }
}
