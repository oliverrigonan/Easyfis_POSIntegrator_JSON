using System;
using System.Linq;
using System.Threading;
using System.Globalization;

namespace POSIntegrator
{
    class Program
    {
        // ============
        // Data Context
        // ============
        private static Data.POSDatabaseDataContext posData;

        // ===========
        // Main Method
        // ===========
        public static void Main(String[] args)
        {
            Int32 i = 0;
            String apiUrlHost = "localhost:2651", database = "pos13";
            foreach (var arg in args)
            {
                if (i == 0) { apiUrlHost = arg; }
                else if (i == 1) { database = arg; }
                i++;
            }

            Console.WriteLine("=================================================");
            Console.WriteLine("Innosoft POS Integrator - Version: 1.20180918.NOR");
            Console.WriteLine("=================================================");

            Console.WriteLine();

            Boolean isConnected = false;

            var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
            posData = new Data.POSDatabaseDataContext(newConnectionString);

            if (!database.Equals(""))
            {
                if (!apiUrlHost.Equals(""))
                {
                    Console.WriteLine("Connecting to server...");

                    if (posData.DatabaseExists())
                    {
                        var sysSettings = from d in posData.SysSettings select d;
                        if (sysSettings.Any())
                        {
                            var branchCode = sysSettings.FirstOrDefault().BranchCode;

                            Console.WriteLine("Connected! Branch Code: " + branchCode);
                            Console.WriteLine("Waiting for transactions...");
                            Console.WriteLine();
                        }

                        isConnected = true;
                    }
                    else
                    {
                        Console.WriteLine("Database not found!");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid URL Host!");
                }
            }
            else
            {
                Console.WriteLine("Invalid Database!");
            }

            Controllers.MstItemController objMstItem = new Controllers.MstItemController();
            Controllers.MstCustomerController objMstCustomer = new Controllers.MstCustomerController();
            Controllers.MstSupplierController objMstSupplier = new Controllers.MstSupplierController();
            Controllers.TrnStockTransferInController objTrnStockTransferIn = new Controllers.TrnStockTransferInController();
            Controllers.TrnStockTransferOutController objTrnStockTransferOut = new Controllers.TrnStockTransferOutController();
            Controllers.TrnStockInController objTrnStockIn = new Controllers.TrnStockInController();
            Controllers.TrnStockOutController objTrnStockOut = new Controllers.TrnStockOutController();
            Controllers.TrnReceivingReceiptController objTrnReceivingReceipt = new Controllers.TrnReceivingReceiptController();
            Controllers.TrnCollectionController objTrnCollection = new Controllers.TrnCollectionController();
            Controllers.TrnSalesReturnController objTrnSalesReturn = new Controllers.TrnSalesReturnController();
            Controllers.TrnItemPriceController objTrnItemPrice = new Controllers.TrnItemPriceController();

            while (true)
            {
                if (isConnected)
                {
                    var sysSettings = from d in posData.SysSettings select d;
                    if (sysSettings.Any())
                    {
                        var branchCode = sysSettings.FirstOrDefault().BranchCode;
                        var userCode = sysSettings.FirstOrDefault().UserCode;

                        // ============
                        // Master Files
                        // ============
                        objMstItem.GetItem(database, apiUrlHost);
                        objMstCustomer.GetCustomer(database, apiUrlHost);
                        objMstSupplier.GetSupplier(database, apiUrlHost);

                        // ==================
                        // Inventory Movement
                        // ==================
                        objTrnReceivingReceipt.GetReceivingReceipt(database, apiUrlHost, branchCode);
                        objTrnStockIn.GetStockIn(database, apiUrlHost, branchCode);
                        objTrnStockOut.GetStockOut(database, apiUrlHost, branchCode);
                        objTrnStockTransferIn.GetStockTransferIN(database, apiUrlHost, branchCode);
                        objTrnStockTransferOut.GetStockTransferOT(database, apiUrlHost, branchCode);

                        // ====================
                        // Sales and Collection
                        // ====================
                        objTrnCollection.GetCollection(database, apiUrlHost, branchCode, userCode);
                        objTrnSalesReturn.GetSalesReturn(database, apiUrlHost, branchCode, userCode);
                        objTrnItemPrice.GetItemPrice(database, apiUrlHost, branchCode);
                    }
                }

                Thread.Sleep(5000);
            }
        }
    }
}