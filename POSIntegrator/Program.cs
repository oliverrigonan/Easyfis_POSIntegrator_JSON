using System;
using System.Linq;
using System.Threading;
using System.Globalization;

namespace POSIntegrator
{
    class Program
    {
        // =============
        // Data Contexts
        // =============
        private static POSdb1.POSdb1DataContext posData1 = new POSdb1.POSdb1DataContext();
        private static POSdb2.POSdb2DataContext posData2 = new POSdb2.POSdb2DataContext();
        private static POSdb3.POSdb3DataContext posData3 = new POSdb3.POSdb3DataContext();

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
            Console.WriteLine("Connecting to server...");

            Boolean isConnected = false;

            if (database.Equals("1"))
            {
                if (posData1.DatabaseExists())
                {
                    var sysSettings = from d in posData1.SysSettings select d;
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
            else if (database.Equals("2"))
            {
                if (posData2.DatabaseExists())
                {
                    var sysSettings = from d in posData2.SysSettings select d;
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
            else if (database.Equals("3"))
            {
                if (posData3.DatabaseExists())
                {
                    var sysSettings = from d in posData3.SysSettings select d;
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
                Console.WriteLine("Database not found!");
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
                    if (database.Equals("1"))
                    {
                        var sysSettings = from d in posData1.SysSettings select d;
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
                    else if (database.Equals("2"))
                    {
                        var sysSettings = from d in posData2.SysSettings select d;
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
                    else
                    {
                        if (database.Equals("3"))
                        {
                            var sysSettings = from d in posData3.SysSettings select d;
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
                    }

                }

                Thread.Sleep(5000);
            }
        }
    }
}
