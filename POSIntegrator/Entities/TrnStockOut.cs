using System;
using System.Collections.Generic;

namespace POSIntegrator
{
    public class TrnStockOut
    {
        public String BranchCode { get; set; }
        public String Branch { get; set; }
        public String OTNumber { get; set; }
        public String OTDate { get; set; }
        public String Particulars { get; set; }
        public String ManualOTNumber { get; set; }
        public String PreparedBy { get; set; }
        public String CheckedBy { get; set; }
        public String ApprovedBy { get; set; }
        public Boolean IsLocked { get; set; }
        public String CreatedBy { get; set; }
        public String CreatedDateTime { get; set; }
        public String UpdatedBy { get; set; }
        public String UpdatedDateTime { get; set; }
        public List<TrnStockOutItem> ListPOSIntegrationTrnStockOutItem { get; set; }
    }
}
