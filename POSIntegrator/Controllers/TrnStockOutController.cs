using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class TrnStockOutController
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

        // =============
        // GET Stock Out
        // =============
        public void GetStockOut(String database, String apiUrlHost, String branchCode)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String stockOutDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/stockOut/" + stockOutDate + "/" + branchCode);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<POSIntegrator.TrnStockOut> stockOutLists = (List<POSIntegrator.TrnStockOut>)js.Deserialize(result, typeof(List<POSIntegrator.TrnStockOut>));

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
                                Amount = stockOutListItem.Amount
                            });
                        }

                        var stockOutData = new POSIntegrator.TrnStockOut()
                        {
                            BranchCode = stockOutList.BranchCode,
                            Branch = stockOutList.Branch,
                            OTNumber = stockOutList.OTNumber,
                            OTDate = stockOutList.OTDate,
                            ListPOSIntegrationTrnStockOutItem = stockOutList.ListPOSIntegrationTrnStockOutItem.ToList()
                        };

                        String jsonPath = "d:/innosoft/json/OT";
                        String fileName = "OT-" + stockOutList.BranchCode + "-" + stockOutList.OTNumber;

                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(stockOutData);
                        String jsonFileName = jsonPath + "\\" + fileName + ".json";
                        File.WriteAllText(jsonFileName, json);

                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        posData = new Data.POSDatabaseDataContext(newConnectionString);

                        var stockOut = from d in posData.TrnStockOuts
                                       where d.Remarks.Equals(fileName)
                                       && d.TrnStockOutLines.Count() > 0
                                       select d;

                        if (!stockOut.Any())
                        {
                            Console.WriteLine("Saving Stock Out...");
                            InsertStockOut(database);
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
                    POSIntegrator.TrnStockOut ot = json_serializer.Deserialize<POSIntegrator.TrnStockOut>(json);

                    var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                    posData = new Data.POSDatabaseDataContext(newConnectionString);

                    String fileName = "OT-" + ot.BranchCode + "-" + ot.OTNumber;
                    var stockOut = from d in posData.TrnStockOuts
                                   where d.Remarks.Equals(fileName)
                                   && d.TrnStockOutLines.Count() > 0
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

                            posData.TrnStockOuts.InsertOnSubmit(newStockOut);
                            posData.SubmitChanges();

                            foreach (var item in ot.ListPOSIntegrationTrnStockOutItem.ToList())
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
                                        Console.WriteLine("Stock Out: " + fileName);
                                        Console.WriteLine("Remarks: " + fileName);
                                        Console.WriteLine("Save Successful!");
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
