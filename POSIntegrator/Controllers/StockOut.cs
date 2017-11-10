using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class StockOut
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

        // =============
        // GET Stock Out
        // =============
        public void GetStockOut(String database, String apiUrlHost, String branchCode)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Today;
                String stockOutDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/stockOut/" + stockOutDate + "/" + branchCode);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<TrnStockOut> stockOutLists = (List<TrnStockOut>)js.Deserialize(result, typeof(List<TrnStockOut>));

                    foreach (var stockOutList in stockOutLists)
                    {
                        List<TrnStockOutItem> listStockOutItems = new List<TrnStockOutItem>();
                        foreach (var stockOutListItem in stockOutList.ListPOSIntegrationTrnStockOutItem)
                        {
                            listStockOutItems.Add(new TrnStockOutItem()
                            {
                                OTId = stockOutListItem.OTId,
                                ItemCode = stockOutListItem.ItemCode,
                                Item = stockOutListItem.Item,
                                Unit = stockOutListItem.Unit,
                                Quantity = stockOutListItem.Quantity,
                                Cost = stockOutListItem.Cost,
                                Amount = stockOutListItem.Amount,
                                BaseUnit = stockOutListItem.BaseUnit,
                                BaseQuantity = stockOutListItem.BaseQuantity,
                                BaseCost = stockOutListItem.BaseCost
                            });
                        }

                        var stockOutData = new TrnStockOut()
                        {
                            BranchCode = stockOutList.BranchCode,
                            Branch = stockOutList.Branch,
                            OTNumber = stockOutList.OTNumber,
                            OTDate = stockOutList.OTDate,
                            Particulars = stockOutList.Particulars,
                            ManualOTNumber = stockOutList.ManualOTNumber,
                            PreparedBy = stockOutList.PreparedBy,
                            CheckedBy = stockOutList.CheckedBy,
                            ApprovedBy = stockOutList.ApprovedBy,
                            IsLocked = stockOutList.IsLocked,
                            CreatedBy = stockOutList.CreatedBy,
                            CreatedDateTime = stockOutList.CreatedDateTime,
                            UpdatedBy = stockOutList.UpdatedBy,
                            UpdatedDateTime = stockOutList.UpdatedDateTime,
                            ListPOSIntegrationTrnStockOutItem = stockOutList.ListPOSIntegrationTrnStockOutItem.ToList()
                        };

                        String jsonPath = "d:/innosoft/json/OT";
                        String fileName = "OT-" + stockOutList.BranchCode + "-" + stockOutList.OTNumber;

                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(stockOutData);
                        String jsonFileName = jsonPath + "\\" + fileName + ".json";
                        File.WriteAllText(jsonFileName, json);

                        if (database.Equals("1"))
                        {
                            var stockOut = from d in posData1.TrnStockOuts
                                           where d.Remarks.Equals(fileName)
                                           select d;

                            if (!stockOut.Any())
                            {
                                Console.WriteLine("Saving Stock Out - " + fileName + "...");
                                InsertStockOut(database);
                            }
                            else
                            {
                                File.Delete(jsonFileName);
                            }
                        }
                        else if (database.Equals("2"))
                        {
                            var stockOut = from d in posData2.TrnStockOuts
                                           where d.Remarks.Equals(fileName)
                                           select d;

                            if (!stockOut.Any())
                            {
                                Console.WriteLine("Saving Stock Out - " + fileName + "...");
                                InsertStockOut(database);
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
                                var stockOut = from d in posData3.TrnStockOuts
                                               where d.Remarks.Equals(fileName)
                                               select d;

                                if (!stockOut.Any())
                                {
                                    Console.WriteLine("Saving Stock Out - " + fileName + "...");
                                    InsertStockOut(database);
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

        // ================
        // INSERT Stock Out
        // ================
        public void InsertStockOut(String database)
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
                    TrnStockOut ot = json_serializer.Deserialize<TrnStockOut>(json);

                    if (database.Equals("1"))
                    {
                        String fileName = "OT-" + ot.BranchCode + "-" + ot.OTNumber;
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
                                    StockOutDate = Convert.ToDateTime(ot.OTDate),
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

                                foreach (var item in ot.ListPOSIntegrationTrnStockOutItem.ToList())
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
                                            Console.WriteLine("Stock Out - " + fileName + " was successfully saved!");
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
                        String fileName = "OT-" + ot.BranchCode + "-" + ot.OTNumber;
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
                                    StockOutDate = Convert.ToDateTime(ot.OTDate),
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

                                foreach (var item in ot.ListPOSIntegrationTrnStockOutItem.ToList())
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
                                            Console.WriteLine("Stock Out - " + fileName + " was successfully saved!");
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
                            String fileName = "OT-" + ot.BranchCode + "-" + ot.OTNumber;
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
                                        StockOutDate = Convert.ToDateTime(ot.OTDate),
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

                                    foreach (var item in ot.ListPOSIntegrationTrnStockOutItem.ToList())
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
                                                Console.WriteLine("Stock Out - " + fileName + " was successfully saved!");
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
