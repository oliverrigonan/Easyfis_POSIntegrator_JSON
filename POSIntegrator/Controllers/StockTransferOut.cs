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
        // ============
        // Data Context
        // ============
        private static Data.POSDatabaseDataContext posData;

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
                DateTime dateTimeToday = DateTime.Now;
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

                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        posData = new Data.POSDatabaseDataContext(newConnectionString);

                        var stockOuts = from d in posData.TrnStockOuts
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

                    var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                    posData = new Data.POSDatabaseDataContext(newConnectionString);

                    String fileName = "ST-" + st.BranchCode + "-" + st.STNumber;
                    var stockOut = from d in posData.TrnStockOuts
                                   where d.Remarks.Equals(fileName)
                                   select d;

                    if (!stockOut.Any())
                    {
                        var defaultPeriod = from d in posData.MstPeriods select d;
                        var defaultSettings = from d in posData.SysSettings select d;

                        var lastStockOutNumber = from d in posData.TrnStockOuts.OrderByDescending(d => d.Id) select d;
                        var stockOutNumberResult = defaultPeriod.FirstOrDefault().Period + "-000001";

                        if (lastStockOutNumber.Any())
                        {
                            var stockOutNumberSplitStrings = lastStockOutNumber.FirstOrDefault().StockOutNumber;
                            Int32 secondIndex = stockOutNumberSplitStrings.IndexOf('-', stockOutNumberSplitStrings.IndexOf('-'));
                            var stockOutNumberSplitStringValue = stockOutNumberSplitStrings.Substring(secondIndex + 1);
                            var stockOutNumber = Convert.ToInt32(stockOutNumberSplitStringValue) + 000001;
                            stockOutNumberResult = defaultPeriod.FirstOrDefault().Period + "-" + FillLeadingZeroes(stockOutNumber, 6);
                        }

                        var accounts = from d in posData.MstAccounts
                                       select d;

                        if (accounts.Any())
                        {
                            Data.TrnStockOut newStockOut = new Data.TrnStockOut
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

                            posData.TrnStockOuts.InsertOnSubmit(newStockOut);
                            posData.SubmitChanges();

                            foreach (var item in st.ListPOSIntegrationTrnStockTransferItem.ToList())
                            {
                                var items = from d in posData.MstItems
                                            where d.BarCode.Equals(item.ItemCode)
                                            select d;

                                if (items.Any())
                                {
                                    var units = from d in posData.MstUnits
                                                where d.Unit.Equals(item.Unit)
                                                select d;

                                    if (units.Any())
                                    {
                                        Data.TrnStockOutLine newStockOutLine = new Data.TrnStockOutLine
                                        {
                                            StockOutId = newStockOut.Id,
                                            ItemId = items.FirstOrDefault().Id,
                                            UnitId = units.FirstOrDefault().Id,
                                            Quantity = item.Quantity,
                                            Cost = item.Cost,
                                            Amount = item.Amount,
                                            AssetAccountId = items.FirstOrDefault().AssetAccountId,
                                        };

                                        posData.TrnStockOutLines.InsertOnSubmit(newStockOutLine);

                                        var currentItem = from d in posData.MstItems
                                                          where d.Id == newStockOutLine.ItemId
                                                          select d;

                                        if (currentItem.Any())
                                        {
                                            Decimal currentOnHandQuantity = currentItem.FirstOrDefault().OnhandQuantity;
                                            Decimal totalQuantity = currentOnHandQuantity - Convert.ToDecimal(item.Quantity);

                                            var updateItem = currentItem.FirstOrDefault();
                                            updateItem.OnhandQuantity = totalQuantity;
                                        }

                                        posData.SubmitChanges();
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
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
