using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class TrnStockTransferInController
    {
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
        // Get Stock Transfer - IN
        // =======================
        public void GetStockTransferIN(String database, String apiUrlHost, String toBranchCode)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String stockTransferDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                // ============
                // Http Request
                // ============
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/stockTransferItems/IN/" + stockTransferDate + "/" + toBranchCode);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                // ================
                // Process Response
                // ================
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<TrnStockTransfer> stockTransferLists = (List<TrnStockTransfer>)js.Deserialize(result, typeof(List<TrnStockTransfer>));

                    if (stockTransferLists.Any())
                    {
                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        Data.POSDatabaseDataContext posData = new Data.POSDatabaseDataContext(newConnectionString);

                        foreach (var stockTransfer in stockTransferLists)
                        {
                            var currentStockIn = from d in posData.TrnStockIns where d.Remarks.Equals("ST-" + stockTransfer.BranchCode + "-" + stockTransfer.STNumber) && d.TrnStockInLines.Count() > 0 select d;
                            if (!currentStockIn.Any())
                            {
                                Console.WriteLine("Saving Stock Transfer (IN): ST-" + stockTransfer.BranchCode + "-" + stockTransfer.STNumber);

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
                                    StockInDate = Convert.ToDateTime(stockTransfer.STDate),
                                    StockInNumber = stockInNumberResult,
                                    SupplierId = defaultSettings.FirstOrDefault().PostSupplierId,
                                    Remarks = "ST-" + stockTransfer.BranchCode + "-" + stockTransfer.STNumber,
                                    IsReturn = false,
                                    PreparedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    CheckedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    ApprovedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    IsLocked = true,
                                    EntryUserId = defaultSettings.FirstOrDefault().PostUserId,
                                    EntryDateTime = DateTime.Now,
                                    UpdateUserId = defaultSettings.FirstOrDefault().PostUserId,
                                    UpdateDateTime = DateTime.Now
                                };

                                posData.TrnStockIns.InsertOnSubmit(newStockIn);
                                posData.SubmitChanges();

                                if (stockTransfer.ListPOSIntegrationTrnStockTransferItem.Any())
                                {
                                    foreach (var item in stockTransfer.ListPOSIntegrationTrnStockTransferItem.ToList())
                                    {
                                        var currentItem = from d in posData.MstItems where d.BarCode.Equals(item.ItemCode) select d;
                                        if (currentItem.Any())
                                        {
                                            var currentItemUnit = from d in posData.MstUnits where d.Unit.Equals(item.Unit) select d;
                                            if (currentItemUnit.Any())
                                            {
                                                Data.TrnStockInLine newStockInLine = new Data.TrnStockInLine
                                                {
                                                    StockInId = newStockIn.Id,
                                                    ItemId = currentItem.FirstOrDefault().Id,
                                                    UnitId = currentItemUnit.FirstOrDefault().Id,
                                                    Quantity = item.Quantity,
                                                    Cost = item.Cost,
                                                    Amount = item.Amount,
                                                    ExpiryDate = currentItem.FirstOrDefault().ExpiryDate,
                                                    LotNumber = currentItem.FirstOrDefault().LotNumber,
                                                    AssetAccountId = currentItem.FirstOrDefault().AssetAccountId,
                                                    Price = currentItem.FirstOrDefault().Price
                                                };

                                                posData.TrnStockInLines.InsertOnSubmit(newStockInLine);

                                                var updateItem = currentItem.FirstOrDefault();
                                                updateItem.OnhandQuantity = currentItem.FirstOrDefault().OnhandQuantity + Convert.ToDecimal(item.Quantity);

                                                posData.SubmitChanges();

                                                Console.WriteLine("* " + currentItem.FirstOrDefault().ItemDescription);
                                            }
                                        }
                                    }
                                }

                                Console.WriteLine("Save Successful!");
                                Console.WriteLine();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}