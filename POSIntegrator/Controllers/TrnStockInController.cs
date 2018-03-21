using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class TrnStockInController
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
        // GET Stock In
        // =============
        public void GetStockIn(String database, String apiUrlHost, String branchCode)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String stockInDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/stockIn/" + stockInDate + "/" + branchCode);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<POSIntegrator.TrnStockIn> stockInLists = (List<POSIntegrator.TrnStockIn>)js.Deserialize(result, typeof(List<POSIntegrator.TrnStockIn>));

                    foreach (var stockInList in stockInLists)
                    {
                        List<TrnStockInItem> listStockInItems = new List<TrnStockInItem>();
                        foreach (var stockInListItem in stockInList.ListPOSIntegrationTrnStockInItem)
                        {
                            listStockInItems.Add(new TrnStockInItem()
                            {
                                INId = stockInListItem.INId,
                                ItemCode = stockInListItem.ItemCode,
                                Item = stockInListItem.Item,
                                Unit = stockInListItem.Unit,
                                Quantity = stockInListItem.Quantity,
                                Cost = stockInListItem.Cost,
                                Amount = stockInListItem.Amount
                            });
                        }

                        var stockInData = new POSIntegrator.TrnStockIn()
                        {
                            BranchCode = stockInList.BranchCode,
                            Branch = stockInList.Branch,
                            INNumber = stockInList.INNumber,
                            INDate = stockInList.INDate,
                            ListPOSIntegrationTrnStockInItem = stockInList.ListPOSIntegrationTrnStockInItem.ToList()
                        };

                        String jsonPath = "d:/innosoft/json/IN";
                        String fileName = "IN-" + stockInList.BranchCode + "-" + stockInList.INNumber;

                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(stockInData);
                        String jsonFileName = jsonPath + "\\" + fileName + ".json";
                        File.WriteAllText(jsonFileName, json);

                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        posData = new Data.POSDatabaseDataContext(newConnectionString);

                        var stockIn = from d in posData.TrnStockIns
                                      where d.Remarks.Equals(fileName)
                                      && d.TrnStockInLines.Count() > 0
                                      select d;

                        if (!stockIn.Any())
                        {
                            Console.WriteLine("Saving Stock In...");
                            InsertStockIn(database);
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
        // INSERT Stock In
        // ================
        public void InsertStockIn(String database)
        {
            try
            {
                String jsonPath = "d:/innosoft/json/IN";
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
                    POSIntegrator.TrnStockIn stockInJson = json_serializer.Deserialize<POSIntegrator.TrnStockIn>(json);

                    var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                    posData = new Data.POSDatabaseDataContext(newConnectionString);

                    String fileName = "IN-" + stockInJson.BranchCode + "-" + stockInJson.INNumber;
                    var stockIn = from d in posData.TrnStockIns
                                  where d.Remarks.Equals(fileName)
                                  && d.TrnStockInLines.Count() > 0
                                  select d;

                    if (!stockIn.Any())
                    {
                        var defaultPeriod = from d in posData.MstPeriods select d;
                        var defaultSettings = from d in posData.SysSettings select d;

                        var lastStockInNumber = from d in posData.TrnStockIns.OrderByDescending(d => d.Id) select d;
                        var stockInNumberResult = defaultPeriod.FirstOrDefault().Period + "-000001";

                        if (lastStockInNumber.Any())
                        {
                            var stockInNumberSplitStrings = lastStockInNumber.FirstOrDefault().StockInNumber;
                            Int32 secondIndex = stockInNumberSplitStrings.IndexOf('-', stockInNumberSplitStrings.IndexOf('-'));
                            var stockInNumberSplitStringValue = stockInNumberSplitStrings.Substring(secondIndex + 1);
                            var stockInNumber = Convert.ToInt32(stockInNumberSplitStringValue) + 000001;
                            stockInNumberResult = defaultPeriod.FirstOrDefault().Period + "-" + FillLeadingZeroes(stockInNumber, 6);
                        }

                        Data.TrnStockIn newStockIn = new Data.TrnStockIn
                        {
                            PeriodId = defaultPeriod.FirstOrDefault().Id,
                            StockInDate = Convert.ToDateTime(stockInJson.INDate),
                            StockInNumber = stockInNumberResult,
                            SupplierId = defaultSettings.FirstOrDefault().PostSupplierId,
                            Remarks = fileName,
                            IsReturn = false,
                            CollectionId = null,
                            PurchaseOrderId = null,
                            PreparedBy = defaultSettings.FirstOrDefault().PostUserId,
                            CheckedBy = defaultSettings.FirstOrDefault().PostUserId,
                            ApprovedBy = defaultSettings.FirstOrDefault().PostUserId,
                            IsLocked = true,
                            EntryUserId = defaultSettings.FirstOrDefault().PostUserId,
                            EntryDateTime = DateTime.Now,
                            UpdateUserId = defaultSettings.FirstOrDefault().PostUserId,
                            UpdateDateTime = DateTime.Now,
                            SalesId = null
                        };

                        posData.TrnStockIns.InsertOnSubmit(newStockIn);
                        posData.SubmitChanges();

                        foreach (var item in stockInJson.ListPOSIntegrationTrnStockInItem.ToList())
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
                                    Data.TrnStockInLine newStockInLine = new Data.TrnStockInLine
                                    {
                                        StockInId = newStockIn.Id,
                                        ItemId = items.FirstOrDefault().Id,
                                        UnitId = units.FirstOrDefault().Id,
                                        Quantity = item.Quantity,
                                        Cost = item.Cost,
                                        Amount = item.Amount,
                                        ExpiryDate = items.FirstOrDefault().ExpiryDate,
                                        LotNumber = items.FirstOrDefault().LotNumber,
                                        AssetAccountId = items.FirstOrDefault().AssetAccountId,
                                        Price = items.FirstOrDefault().Price
                                    };

                                    posData.TrnStockInLines.InsertOnSubmit(newStockInLine);

                                    var currentItem = from d in posData.MstItems
                                                      where d.Id == newStockInLine.ItemId
                                                      select d;

                                    if (currentItem.Any())
                                    {
                                        Decimal currentOnHandQuantity = currentItem.FirstOrDefault().OnhandQuantity;
                                        Decimal totalQuantity = currentOnHandQuantity + Convert.ToDecimal(item.Quantity);

                                        var updateItem = currentItem.FirstOrDefault();
                                        updateItem.OnhandQuantity = totalQuantity;
                                    }

                                    posData.SubmitChanges();

                                    Console.WriteLine("Stock In: " + fileName);
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
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
