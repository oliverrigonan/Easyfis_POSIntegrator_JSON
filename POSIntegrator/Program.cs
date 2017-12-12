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
            String apiUrlHost = "localhost:2651", database = "pos13_3abuilders";
            foreach (var arg in args)
            {
                if (i == 0) { apiUrlHost = arg; }
                else if (i == 1) { database = arg; }
                i++;
            }

            Console.WriteLine("===========================================");
            Console.WriteLine("Innosoft POS Uploader - Version: 1.20171107");
            Console.WriteLine("===========================================");

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

            Controllers.TrnCollectionController objCollection = new Controllers.TrnCollectionController();
            Controllers.TrnStockTransferInController objStockTransferIn = new Controllers.TrnStockTransferInController();
            Controllers.TrnStockTransferOutController objStockTransferOut = new Controllers.TrnStockTransferOutController();
            Controllers.TrnStockOutController objStockOut = new Controllers.TrnStockOutController();
            Controllers.TrnReceivingReceiptController objReceivingReceipt = new Controllers.TrnReceivingReceiptController();
            Controllers.MstItemController objItem = new Controllers.MstItemController();
            Controllers.MstCustomerController objCustomer = new Controllers.MstCustomerController();
            Controllers.MstSupplierController objSupplier = new Controllers.MstSupplierController();

            while (true)
            {
                if (isConnected)
                {
                    var sysSettings = from d in posData.SysSettings select d;
                    if (sysSettings.Any())
                    {
                        var branchCode = sysSettings.FirstOrDefault().BranchCode;
                        var userCode = sysSettings.FirstOrDefault().UserCode;

                        objCollection.GetCollection(database, apiUrlHost, branchCode, userCode);
                        objStockTransferIn.GetStockTransferIN(database, apiUrlHost, branchCode);
                        objStockTransferOut.GetStockTransferOT(database, apiUrlHost, branchCode);
                        objStockOut.GetStockOut(database, apiUrlHost, branchCode);
                        objReceivingReceipt.GetReceivingReceipt(database, apiUrlHost, branchCode);
                        objItem.GetItem(database, apiUrlHost);
                        objCustomer.GetCustomer(database, apiUrlHost);
                        objSupplier.GetSupplier(database, apiUrlHost);
                    }
                }

                Thread.Sleep(5000);
            }
        }
    }
}
