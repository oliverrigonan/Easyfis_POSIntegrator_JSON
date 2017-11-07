using System;
using System.Collections.Generic;

namespace POSIntegrator
{
    public class TrnCollection
    {
        public String SIDate { get; set; }
        public String BranchCode { get; set; }
        public String CustomerManualArticleCode { get; set; }
        public String CreatedBy { get; set; }
        public String Term { get; set; }
        public String DocumentReference { get; set; }
        public String ManualSINumber { get; set; }
        public String Remarks { get; set; }
        public List<TrnCollectionLines> ListPOSIntegrationTrnSalesInvoiceItem { get; set; }
    }
}
