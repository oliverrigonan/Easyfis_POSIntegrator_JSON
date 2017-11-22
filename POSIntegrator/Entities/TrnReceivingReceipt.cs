using System;
using System.Collections.Generic;

namespace POSIntegrator
{
    public class TrnReceivingReceipt
    {
        public String BranchCode { get; set; }
        public String Branch { get; set; }
        public String RRNumber { get; set; }
        public String RRDate { get; set; }
        public String Supplier { get; set; }
        public String Term { get; set; }
        public String DocumentReference { get; set; }
        public String ManualRRNumber { get; set; }
        public String Remarks { get; set; }
        public String ReceivedBy { get; set; }
        public String PreparedBy { get; set; }
        public String CheckedBy { get; set; }
        public String ApprovedBy { get; set; }
        public Boolean IsLocked { get; set; }
        public String CreatedBy { get; set; }
        public String CreatedDateTime { get; set; }
        public String UpdatedBy { get; set; }
        public String UpdatedDateTime { get; set; }
        public List<TrnReceivingReceiptItem> ListPOSIntegrationTrnReceivingReceiptItem { get; set; }
    }
}
