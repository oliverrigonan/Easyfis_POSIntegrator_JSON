using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class StockTransferOut
    {
        // =============
        // Data Contexts
        // =============
        private static POSdb1.POSdb1DataContext posData1 = new POSdb1.POSdb1DataContext();
        private static POSdb2.POSdb2DataContext posData2 = new POSdb2.POSdb2DataContext();
        private static POSdb3.POSdb3DataContext posData3 = new POSdb3.POSdb3DataContext();

        // ===================
        // Fill Leading Zeroes
        // ===================
        public String FillLeadingZeroes(Int32 number, Int32 length)
        {
            var result = number.ToString();
            var pad = length - result.Length;
            while (pad > 0)
            {
                result = '0' + result;
                pad--;
            }

            return result;
        }

        // =======================
        // GET Stock Transfer - OT
        // =======================
        public void GetStockTransferOT(String database, String apiUrlHost, String fromBranchCode)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Today;
                String stockTransferDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/stockTransferItems/OT/" + stockTransferDate + "/" + fromBranchCode);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<TrnStockTransfer> stockTransferLists = (List<TrnStockTransfer>)js.Deserialize(result, typeof(List<TrnStockTransfer>));

                    foreach (var stockTransferList in stockTransferLists)
                    {
                        List<TrnStockTransferItem> listStockTransferItems = new List<TrnStockTransferItem>();
                        foreach (var stockTransferListItem in stockTransferList.ListPOSIntegrationTrnStockTransferItem)
                        {
                            listStockTransferItems.Add(new TrnStockTransferItem()
                            {
                                STId = stockTransferListItem.STId,
                                ItemCode = stockTransferListItem.ItemCode,
                                Item = stockTransferListItem.Item,
                                InventoryCode = stockTransferListItem.InventoryCode,
                                Particulars = stockTransferListItem.Particulars,
                                Unit = stockTransferListItem.Unit,
                                Quantity = stockTransferListItem.Quantity,
                                Cost = stockTransferListItem.Cost,
                                Amount = stockTransferListItem.Amount,
                                BaseUnit = stockTransferListItem.BaseUnit,
                                BaseQuantity = stockTransferListItem.BaseQuantity,
                                BaseCost = stockTransferListItem.BaseCost
                            });
                        }

                        var stockTransferData = new TrnStockTransfer()
                        {
                            BranchCode = stockTransferList.BranchCode,
                            Branch = stockTransferList.Branch,
                            STNumber = stockTransferList.STNumber,
                            STDate = stockTransferList.STDate,
                            ToBranch = stockTransferList.ToBranch,
                            ToBranchCode = stockTransferList.ToBranchCode,
                            Article = stockTransferList.Article,
                            Particulars = stockTransferList.Particulars,
                            ManualSTNumber = stockTransferList.ManualSTNumber,
                            PreparedBy = stockTransferList.PreparedBy,
                            CheckedBy = stockTransferList.CheckedBy,
                            ApprovedBy = stockTransferList.ApprovedBy,
                            IsLocked = stockTransferList.IsLocked,
                            CreatedBy = stockTransferList.CreatedBy,
                            CreatedDateTime = stockTransferList.CreatedDateTime,
                            UpdatedBy = stockTransferList.UpdatedBy,
                            UpdatedDateTime = stockTransferList.UpdatedDateTime,
                            ListPOSIntegrationTrnStockTransferItem = stockTransferList.ListPOSIntegrationTrnStockTransferItem.ToList()
                        };

                        String jsonPath = "d:/innosoft/json/OT";
                        String fileName = "ST-" + stockTransferList.BranchCode + "-" + stockTransferList.STNumber;

                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(stockTransferData);
                        String jsonFileName = jsonPath + "\\" + fileName + ".json";
                        File.WriteAllText(jsonFileName, json);

                        if (database.Equals("1"))
                        {
                            var stockOuts = from d in posData1.TrnStockOuts
                                            where d.Remarks.Equals(fileName)
                                            select d;

                            if (!stockOuts.Any())
                            {
                                Console.WriteLine("Saving Stock Transfer (OT) - " + fileName + "...");
                                InsertStockTransferOT(database);
                            }
                            else
                            {
                                File.Delete(jsonFileName);
                            }
                        }
                        else if (database.Equals("2"))
                        {
                            var stockOuts = from d in posData2.TrnStockOuts
                                            where d.Remarks.Equals(fileName)
                                            select d;

                            if (!stockOuts.Any())
                            {
                                Console.WriteLine("Saving Stock Transfer (OT) - " + fileName + "...");
                                InsertStockTransferOT(database);
                            }
                            else
                            {
                                File.Delete(jsonFileName);
                            }
                        }
                        else
                        {
                            if (database.Equals("3"))
                            {
                                var stockOuts = from d in posData3.TrnStockOuts
                                                where d.Remarks.Equals(fileName)
                                                select d;

                                if (!stockOuts.Any())
                                {
                                    Console.WriteLine("Saving Stock Transfer (OT) - " + fileName + "...");
                                    InsertStockTransferOT(database);
                                }
                                else
                                {
                                    File.Delete(jsonFileName);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // ==========================
        // INSERT Stock Transfer - OT
        // ==========================
        public void InsertStockTransferOT(String database)
        {
            try
            {
                String jsonPath = "d:/innosoft/json/OT";
                List<String> files = new List<String>(Directory.EnumerateFiles(jsonPath));
                foreach (var file in files)
                {
                    // ==============
                    // Read json file
                    // ==============
                    String json;
                    using (StreamReader r = new StreamReader(file))
                    {
                        json = r.ReadToEnd();
                    }

                    var json_serializer = new JavaScriptSerializer();
                    TrnStockTransfer st = json_serializer.Deserialize<TrnStockTransfer>(json);

                    if (database.Equals("1"))
                    {
                        String fileName = "ST-" + st.BranchCode + "-" + st.STNumber;
                        var stockOut = from d in posData1.TrnStockOuts
                                       where d.Remarks.Equals(fileName)
                                       select d;

                        if (!stockOut.Any())
                        {
                            var defaultPeriod = from d in posData1.MstPeriods select d;
                            var defaultSettings = from d in posData1.SysSettings select d;

                            var lastStockOutNumber = from d in posData1.TrnStockOuts.OrderByDescending(d => d.Id) select d;
                            var stockOutNumberResult = defaultPeriod.FirstOrDefault().Period + "-000001";

                            if (lastStockOutNumber.Any())
                            {
                                var stockOutNumberSplitStrings = lastStockOutNumber.FirstOrDefault().StockOutNumber;
                                Int32 secondIndex = stockOutNumberSplitStrings.IndexOf('-', stockOutNumberSplitStrings.IndexOf('-'));
                                var stockOutNumberSplitStringValue = stockOutNumberSplitStrings.Substring(secondIndex + 1);
                                var stockOutNumber = Convert.ToInt32(stockOutNumberSplitStringValue) + 000001;
                                stockOutNumberResult = defaultPeriod.FirstOrDefault().Period + "-" + FillLeadingZeroes(stockOutNumber, 6);
                            }

                            var accounts = from d in posData1.MstAccounts
                                           select d;

                            if (accounts.Any())
                            {
                                POSdb1.TrnStockOut newStockOut = new POSdb1.TrnStockOut
                                {
                                    PeriodId = defaultPeriod.FirstOrDefault().Id,
                                    StockOutDate = Convert.ToDateTime(st.STDate),
                                    StockOutNumber = stockOutNumberResult,
                                    AccountId = accounts.FirstOrDefault().Id,
                                    Remarks = fileName,
                                    PreparedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    CheckedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    ApprovedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    IsLocked = true,
                                    EntryUserId = defaultSettings.FirstOrDefault().PostUserId,
                                    EntryDateTime = DateTime.Now,
                                    UpdateUserId = defaultSettings.FirstOrDefault().PostUserId,
                                    UpdateDateTime = DateTime.Now,
                                };

                                posData1.TrnStockOuts.InsertOnSubmit(newStockOut);
                                posData1.SubmitChanges();

                                foreach (var item in st.ListPOSIntegrationTrnStockTransferItem.ToList())
                                {
                                    var items = from d in posData1.MstItems
                                                where d.BarCode.Equals(item.ItemCode)
                                                select d;

                                    if (items.Any())
                                    {
                                        var units = from d in posData1.MstUnits
                                                    where d.Unit.Equals(item.Unit)
                                                    select d;

                                        if (units.Any())
                                        {
                                            POSdb1.TrnStockOutLine newStockOutLine = new POSdb1.TrnStockOutLine
                                            {
                                                StockOutId = newStockOut.Id,
                                                ItemId = items.FirstOrDefault().Id,
                                                UnitId = units.FirstOrDefault().Id,
                                                Quantity = item.Quantity,
                                                Cost = item.Cost,
                                                Amount = item.Amount,
                                                AssetAccountId = items.FirstOrDefault().AssetAccountId,
                                            };

                                            posData1.TrnStockOutLines.InsertOnSubmit(newStockOutLine);

                                            var currentItem = from d in posData1.MstItems
                                                              where d.Id == newStockOutLine.ItemId
                                                              select d;

                                            if (currentItem.Any())
                                            {
                                                Decimal currentOnHandQuantity = currentItem.FirstOrDefault().OnhandQuantity;
                                                Decimal totalQuantity = currentOnHandQuantity - Convert.ToDecimal(item.Quantity);

                                                var updateItem = currentItem.FirstOrDefault();
                                                updateItem.OnhandQuantity = totalQuantity;
                                            }

                                            posData1.SubmitChanges();
                                            Console.WriteLine("Stock Transfer (OT) - " + fileName + " was successfully saved!");
                                            Console.WriteLine("Remarks: " + fileName);
                                            Console.WriteLine();

                                            File.Delete(file);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (database.Equals("2"))
                    {
                        String fileName = "ST-" + st.BranchCode + "-" + st.STNumber;
                        var stockOut = from d in posData2.TrnStockOuts
                                       where d.Remarks.Equals(fileName)
                                       select d;

                        if (!stockOut.Any())
                        {
                            var defaultPeriod = from d in posData2.MstPeriods select d;
                            var defaultSettings = from d in posData2.SysSettings select d;

                            var lastStockOutNumber = from d in posData2.TrnStockOuts.OrderByDescending(d => d.Id) select d;
                            var stockOutNumberResult = defaultPeriod.FirstOrDefault().Period + "-000001";

                            if (lastStockOutNumber.Any())
                            {
                                var stockOutNumberSplitStrings = lastStockOutNumber.FirstOrDefault().StockOutNumber;
                                Int32 secondIndex = stockOutNumberSplitStrings.IndexOf('-', stockOutNumberSplitStrings.IndexOf('-'));
                                var stockOutNumberSplitStringValue = stockOutNumberSplitStrings.Substring(secondIndex + 1);
                                var stockOutNumber = Convert.ToInt32(stockOutNumberSplitStringValue) + 000001;
                                stockOutNumberResult = defaultPeriod.FirstOrDefault().Period + "-" + FillLeadingZeroes(stockOutNumber, 6);
                            }

                            var accounts = from d in posData2.MstAccounts
                                           select d;

                            if (accounts.Any())
                            {
                                POSdb2.TrnStockOut newStockOut = new POSdb2.TrnStockOut
                                {
                                    PeriodId = defaultPeriod.FirstOrDefault().Id,
                                    StockOutDate = Convert.ToDateTime(st.STDate),
                                    StockOutNumber = stockOutNumberResult,
                                    AccountId = accounts.FirstOrDefault().Id,
                                    Remarks = fileName,
                                    PreparedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    CheckedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    ApprovedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    IsLocked = true,
                                    EntryUserId = defaultSettings.FirstOrDefault().PostUserId,
                                    EntryDateTime = DateTime.Now,
                                    UpdateUserId = defaultSettings.FirstOrDefault().PostUserId,
                                    UpdateDateTime = DateTime.Now,
                                };

                                posData2.TrnStockOuts.InsertOnSubmit(newStockOut);
                                posData2.SubmitChanges();

                                foreach (var item in st.ListPOSIntegrationTrnStockTransferItem.ToList())
                                {
                                    var items = from d in posData2.MstItems
                                                where d.BarCode.Equals(item.ItemCode)
                                                select d;

                                    if (items.Any())
                                    {
                                        var units = from d in posData2.MstUnits
                                                    where d.Unit.Equals(item.Unit)
                                                    select d;

                                        if (units.Any())
                                        {
                                            POSdb2.TrnStockOutLine newStockOutLine = new POSdb2.TrnStockOutLine
                                            {
                                                StockOutId = newStockOut.Id,
                                                ItemId = items.FirstOrDefault().Id,
                                                UnitId = units.FirstOrDefault().Id,
                                                Quantity = item.Quantity,
                                                Cost = item.Cost,
                                                Amount = item.Amount,
                                                AssetAccountId = items.FirstOrDefault().AssetAccountId,
                                            };

                                            posData2.TrnStockOutLines.InsertOnSubmit(newStockOutLine);

                                            var currentItem = from d in posData2.MstItems
                                                              where d.Id == newStockOutLine.ItemId
                                                              select d;

                                            if (currentItem.Any())
                                            {
                                                Decimal currentOnHandQuantity = currentItem.FirstOrDefault().OnhandQuantity;
                                                Decimal totalQuantity = currentOnHandQuantity - Convert.ToDecimal(item.Quantity);

                                                var updateItem = currentItem.FirstOrDefault();
                                                updateItem.OnhandQuantity = totalQuantity;
                                            }

                                            posData2.SubmitChanges();
                                            Console.WriteLine("Stock Transfer (OT) - " + fileName + " was successfully saved!");
                                            Console.WriteLine("Remarks: " + fileName);
                                            Console.WriteLine();

                                            File.Delete(file);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (database.Equals("3"))
                        {
                            String fileName = "ST-" + st.BranchCode + "-" + st.STNumber;
                            var stockOut = from d in posData3.TrnStockOuts
                                           where d.Remarks.Equals(fileName)
                                           select d;

                            if (!stockOut.Any())
                            {
                                var defaultPeriod = from d in posData3.MstPeriods select d;
                                var defaultSettings = from d in posData3.SysSettings select d;

                                var lastStockOutNumber = from d in posData3.TrnStockOuts.OrderByDescending(d => d.Id) select d;
                                var stockOutNumberResult = defaultPeriod.FirstOrDefault().Period + "-000001";

                                if (lastStockOutNumber.Any())
                                {
                                    var stockOutNumberSplitStrings = lastStockOutNumber.FirstOrDefault().StockOutNumber;
                                    Int32 secondIndex = stockOutNumberSplitStrings.IndexOf('-', stockOutNumberSplitStrings.IndexOf('-'));
                                    var stockOutNumberSplitStringValue = stockOutNumberSplitStrings.Substring(secondIndex + 1);
                                    var stockOutNumber = Convert.ToInt32(stockOutNumberSplitStringValue) + 000001;
                                    stockOutNumberResult = defaultPeriod.FirstOrDefault().Period + "-" + FillLeadingZeroes(stockOutNumber, 6);
                                }

                                var accounts = from d in posData3.MstAccounts
                                               select d;

                                if (accounts.Any())
                                {
                                    POSdb3.TrnStockOut newStockOut = new POSdb3.TrnStockOut
                                    {
                                        PeriodId = defaultPeriod.FirstOrDefault().Id,
                                        StockOutDate = Convert.ToDateTime(st.STDate),
                                        StockOutNumber = stockOutNumberResult,
                                        AccountId = accounts.FirstOrDefault().Id,
                                        Remarks = fileName,
                                        PreparedBy = defaultSettings.FirstOrDefault().PostUserId,
                                        CheckedBy = defaultSettings.FirstOrDefault().PostUserId,
                                        ApprovedBy = defaultSettings.FirstOrDefault().PostUserId,
                                        IsLocked = true,
                                        EntryUserId = defaultSettings.FirstOrDefault().PostUserId,
                                        EntryDateTime = DateTime.Now,
                                        UpdateUserId = defaultSettings.FirstOrDefault().PostUserId,
                                        UpdateDateTime = DateTime.Now,
                                    };

                                    posData3.TrnStockOuts.InsertOnSubmit(newStockOut);
                                    posData3.SubmitChanges();

                                    foreach (var item in st.ListPOSIntegrationTrnStockTransferItem.ToList())
                                    {
                                        var items = from d in posData3.MstItems
                                                    where d.BarCode.Equals(item.ItemCode)
                                                    select d;

                                        if (items.Any())
                                        {
                                            var units = from d in posData3.MstUnits
                                                        where d.Unit.Equals(item.Unit)
                                                        select d;

                                            if (units.Any())
                                            {
                                                POSdb3.TrnStockOutLine newStockOutLine = new POSdb3.TrnStockOutLine
                                                {
                                                    StockOutId = newStockOut.Id,
                                                    ItemId = items.FirstOrDefault().Id,
                                                    UnitId = units.FirstOrDefault().Id,
                                                    Quantity = item.Quantity,
                                                    Cost = item.Cost,
                                                    Amount = item.Amount,
                                                    AssetAccountId = items.FirstOrDefault().AssetAccountId,
                                                };

                                                posData3.TrnStockOutLines.InsertOnSubmit(newStockOutLine);

                                                var currentItem = from d in posData3.MstItems
                                                                  where d.Id == newStockOutLine.ItemId
                                                                  select d;

                                                if (currentItem.Any())
                                                {
                                                    Decimal currentOnHandQuantity = currentItem.FirstOrDefault().OnhandQuantity;
                                                    Decimal totalQuantity = currentOnHandQuantity - Convert.ToDecimal(item.Quantity);

                                                    var updateItem = currentItem.FirstOrDefault();
                                                    updateItem.OnhandQuantity = totalQuantity;
                                                }

                                                posData3.SubmitChanges();
                                                Console.WriteLine("Stock Transfer (OT) - " + fileName + " was successfully saved!");
                                                Console.WriteLine("Remarks: " + fileName);
                                                Console.WriteLine();

                                                File.Delete(file);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
