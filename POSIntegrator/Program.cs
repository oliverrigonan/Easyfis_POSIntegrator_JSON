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
            String apiUrlHost = "", database = "";
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

            Controllers.Collection objCollection = new Controllers.Collection();
            Controllers.StockTransferIn objStockTransferIn = new Controllers.StockTransferIn();
            Controllers.StockTransferOut objStockTransferOut = new Controllers.StockTransferOut();
            Controllers.StockOut objStockOut = new Controllers.StockOut();
            Controllers.ReceivingReceipt objReceivingReceipt = new Controllers.ReceivingReceipt();

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
                    }
                }

                Thread.Sleep(5000);
            }
        }
    }
}
