using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSIntegrator
{
    class TrnStockIn
    {
        public String BranchCode { get; set; }
        public String Branch { get; set; }
        public String INNumber { get; set; }
        public String INDate { get; set; }
        public List<TrnStockInItem> ListPOSIntegrationTrnStockInItem { get; set; }
    }
}
