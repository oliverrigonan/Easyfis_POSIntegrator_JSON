using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class ReceivingReceipt
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

        // =====================
        // GET Receiving Receipt
        // =====================
        public void GetReceivingReceipt(String database, String apiUrlHost, String branchCode)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Today;
                String receivingReceiptDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/receivingReceipt/" + receivingReceiptDate + "/" + branchCode);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<TrnReceivingReceipt> receivingReceiptLists = (List<TrnReceivingReceipt>)js.Deserialize(result, typeof(List<TrnReceivingReceipt>));

                    foreach (var receivingReceiptList in receivingReceiptLists)
                    {
                        List<TrnReceivingReceiptItem> listReceivingReceiptItem = new List<TrnReceivingReceiptItem>();
                        foreach (var receivingReceiptListItem in receivingReceiptList.ListPOSIntegrationTrnReceivingReceiptItem)
                        {
                            listReceivingReceiptItem.Add(new TrnReceivingReceiptItem()
                            {
                                RRId = receivingReceiptListItem.RRId,
                                ItemCode = receivingReceiptListItem.ItemCode,
                                Item = receivingReceiptListItem.Item,
                                Particulars = receivingReceiptListItem.Particulars,
                                Unit = receivingReceiptListItem.Unit,
                                Quantity = receivingReceiptListItem.Quantity,
                                Cost = receivingReceiptListItem.Cost,
                                Amount = receivingReceiptListItem.Amount,
                                BaseUnit = receivingReceiptListItem.BaseUnit,
                                BaseQuantity = receivingReceiptListItem.BaseQuantity,
                                BaseCost = receivingReceiptListItem.BaseCost
                            });
                        }

                        var stockTransferData = new TrnReceivingReceipt()
                        {
                            BranchCode = receivingReceiptList.BranchCode,
                            Branch = receivingReceiptList.Branch,
                            RRNumber = receivingReceiptList.RRNumber,
                            RRDate = receivingReceiptList.RRDate,
                            Supplier = receivingReceiptList.Supplier,
                            Term = receivingReceiptList.Term,
                            DocumentReference = receivingReceiptList.DocumentReference,
                            ManualRRNumber = receivingReceiptList.ManualRRNumber,
                            Remarks = receivingReceiptList.Remarks,
                            PreparedBy = receivingReceiptList.PreparedBy,
                            ReceivedBy = receivingReceiptList.PreparedBy,
                            CheckedBy = receivingReceiptList.CheckedBy,
                            ApprovedBy = receivingReceiptList.ApprovedBy,
                            IsLocked = receivingReceiptList.IsLocked,
                            CreatedBy = receivingReceiptList.CreatedBy,
                            CreatedDateTime = receivingReceiptList.CreatedDateTime,
                            UpdatedBy = receivingReceiptList.UpdatedBy,
                            UpdatedDateTime = receivingReceiptList.UpdatedDateTime,
                            ListPOSIntegrationTrnReceivingReceiptItem = receivingReceiptList.ListPOSIntegrationTrnReceivingReceiptItem.ToList()
                        };

                        String jsonPath = "d:/innosoft/json/RR";
                        String fileName = "RR-" + receivingReceiptList.BranchCode + "-" + receivingReceiptList.RRNumber;

                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(stockTransferData);
                        String jsonFileName = jsonPath + "\\" + fileName + ".json";
                        File.WriteAllText(jsonFileName, json);

                        if (database.Equals("1"))
                        {
                            var stockIn = from d in posData1.TrnStockIns
                                          where d.Remarks.Equals(fileName)
                                          select d;

                            if (!stockIn.Any())
                            {
                                Console.WriteLine("Saving Receiving Receipt - " + fileName + "...");
                                InsertReceivingReceipt(database);
                            }
                            else
                            {
                                File.Delete(jsonFileName);
                            }
                        }
                        else if (database.Equals("2"))
                        {
                            var stockIn = from d in posData2.TrnStockIns
                                          where d.Remarks.Equals(fileName)
                                          select d;

                            if (!stockIn.Any())
                            {
                                Console.WriteLine("Saving Receiving Receipt - " + fileName + "...");
                                InsertReceivingReceipt(database);
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
                                var stockIn = from d in posData3.TrnStockIns
                                              where d.Remarks.Equals(fileName)
                                              select d;

                                if (!stockIn.Any())
                                {
                                    Console.WriteLine("Saving Receiving Receipt - " + fileName + "...");
                                    InsertReceivingReceipt(database);
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

        // ========================
        // INSERT Receiving Receipt
        // ========================
        public void InsertReceivingReceipt(String database)
        {
            try
            {
                String jsonPath = "d:/innosoft/json/RR";
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
                    TrnReceivingReceipt rr = json_serializer.Deserialize<TrnReceivingReceipt>(json);

                    if (database.Equals("1"))
                    {
                        String fileName = "RR-" + rr.BranchCode + "-" + rr.RRNumber;
                        var stockIn = from d in posData1.TrnStockIns
                                      where d.Remarks.Equals(fileName)
                                      select d;

                        if (!stockIn.Any())
                        {
                            var defaultPeriod = from d in posData1.MstPeriods select d;
                            var defaultSettings = from d in posData1.SysSettings select d;

                            var lastStockInNumber = from d in posData1.TrnStockIns.OrderByDescending(d => d.Id) select d;
                            var stockInNumberResult = defaultPeriod.FirstOrDefault().Period + "-000001";

                            if (lastStockInNumber.Any())
                            {
                                var stockInNumberSplitStrings = lastStockInNumber.FirstOrDefault().StockInNumber;
                                Int32 secondIndex = stockInNumberSplitStrings.IndexOf('-', stockInNumberSplitStrings.IndexOf('-'));
                                var stockInNumberSplitStringValue = stockInNumberSplitStrings.Substring(secondIndex + 1);
                                var stockInNumber = Convert.ToInt32(stockInNumberSplitStringValue) + 000001;
                                stockInNumberResult = defaultPeriod.FirstOrDefault().Period + "-" + FillLeadingZeroes(stockInNumber, 6);
                            }

                            POSdb1.TrnStockIn newStockIn = new POSdb1.TrnStockIn
                            {
                                PeriodId = defaultPeriod.FirstOrDefault().Id,
                                StockInDate = Convert.ToDateTime(rr.RRDate),
                                StockInNumber = stockInNumberResult,
                                SupplierId = defaultSettings.FirstOrDefault().PostSupplierId,
                                Remarks = fileName,
                                IsReturn = false,
                                CollectionId = null,
                                PurchaseOrderId = null,
                                PreparedBy = defaultSettings.FirstOrDefault().PostUserId,
                                CheckedBy = defaultSettings.FirstOrDefault().PostUserId,
                                ApprovedBy = defaultSettings.FirstOrDefault().PostUserId,
                                IsLocked = 1,
                                EntryUserId = defaultSettings.FirstOrDefault().PostUserId,
                                EntryDateTime = DateTime.Now,
                                UpdateUserId = defaultSettings.FirstOrDefault().PostUserId,
                                UpdateDateTime = DateTime.Now,
                                SalesId = null
                            };

                            posData1.TrnStockIns.InsertOnSubmit(newStockIn);
                            posData1.SubmitChanges();

                            foreach (var item in rr.ListPOSIntegrationTrnReceivingReceiptItem.ToList())
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
                                        POSdb1.TrnStockInLine newStockInLine = new POSdb1.TrnStockInLine
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

                                        posData1.TrnStockInLines.InsertOnSubmit(newStockInLine);

                                        var currentItem = from d in posData1.MstItems
                                                          where d.Id == newStockInLine.ItemId
                                                          select d;

                                        if (currentItem.Any())
                                        {
                                            Decimal currentOnHandQuantity = currentItem.FirstOrDefault().OnhandQuantity;
                                            Decimal totalQuantity = currentOnHandQuantity + Convert.ToDecimal(item.Quantity);

                                            var updateItem = currentItem.FirstOrDefault();
                                            updateItem.OnhandQuantity = totalQuantity;
                                        }

                                        posData1.SubmitChanges();
                                        Console.WriteLine("Receiving Receipt - " + fileName + " was successfully saved!");
                                        Console.WriteLine("Remarks: " + fileName);
                                        Console.WriteLine();

                                        File.Delete(file);
                                    }
                                }
                            }
                        }
                    }
                    else if (database.Equals("2"))
                    {
                        String fileName = "RR-" + rr.BranchCode + "-" + rr.RRNumber;
                        var stockIn = from d in posData2.TrnStockIns
                                      where d.Remarks.Equals(fileName)
                                      select d;

                        if (!stockIn.Any())
                        {
                            var defaultPeriod = from d in posData2.MstPeriods select d;
                            var defaultSettings = from d in posData2.SysSettings select d;

                            var lastStockInNumber = from d in posData2.TrnStockIns.OrderByDescending(d => d.Id) select d;
                            var stockInNumberResult = defaultPeriod.FirstOrDefault().Period + "-000001";

                            if (lastStockInNumber.Any())
                            {
                                var stockInNumberSplitStrings = lastStockInNumber.FirstOrDefault().StockInNumber;
                                Int32 secondIndex = stockInNumberSplitStrings.IndexOf('-', stockInNumberSplitStrings.IndexOf('-'));
                                var stockInNumberSplitStringValue = stockInNumberSplitStrings.Substring(secondIndex + 1);
                                var stockInNumber = Convert.ToInt32(stockInNumberSplitStringValue) + 000001;
                                stockInNumberResult = defaultPeriod.FirstOrDefault().Period + "-" + FillLeadingZeroes(stockInNumber, 6);
                            }

                            POSdb2.TrnStockIn newStockIn = new POSdb2.TrnStockIn
                            {
                                PeriodId = defaultPeriod.FirstOrDefault().Id,
                                StockInDate = Convert.ToDateTime(rr.RRDate),
                                StockInNumber = stockInNumberResult,
                                SupplierId = defaultSettings.FirstOrDefault().PostSupplierId,
                                Remarks = fileName,
                                IsReturn = false,
                                CollectionId = null,
                                PurchaseOrderId = null,
                                PreparedBy = defaultSettings.FirstOrDefault().PostUserId,
                                CheckedBy = defaultSettings.FirstOrDefault().PostUserId,
                                ApprovedBy = defaultSettings.FirstOrDefault().PostUserId,
                                IsLocked = 1,
                                EntryUserId = defaultSettings.FirstOrDefault().PostUserId,
                                EntryDateTime = DateTime.Now,
                                UpdateUserId = defaultSettings.FirstOrDefault().PostUserId,
                                UpdateDateTime = DateTime.Now,
                                SalesId = null
                            };

                            posData2.TrnStockIns.InsertOnSubmit(newStockIn);
                            posData2.SubmitChanges();

                            foreach (var item in rr.ListPOSIntegrationTrnReceivingReceiptItem.ToList())
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
                                        POSdb2.TrnStockInLine newStockInLine = new POSdb2.TrnStockInLine
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

                                        posData2.TrnStockInLines.InsertOnSubmit(newStockInLine);

                                        var currentItem = from d in posData2.MstItems
                                                          where d.Id == newStockInLine.ItemId
                                                          select d;

                                        if (currentItem.Any())
                                        {
                                            Decimal currentOnHandQuantity = currentItem.FirstOrDefault().OnhandQuantity;
                                            Decimal totalQuantity = currentOnHandQuantity + Convert.ToDecimal(item.Quantity);

                                            var updateItem = currentItem.FirstOrDefault();
                                            updateItem.OnhandQuantity = totalQuantity;
                                        }

                                        posData2.SubmitChanges();
                                        Console.WriteLine("Receiving Receipt - " + fileName + " was successfully saved!");
                                        Console.WriteLine("Remarks: " + fileName);
                                        Console.WriteLine();

                                        File.Delete(file);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (database.Equals("3"))
                        {
                            String fileName = "RR-" + rr.BranchCode + "-" + rr.RRNumber;
                            var stockIn = from d in posData3.TrnStockIns
                                          where d.Remarks.Equals(fileName)
                                          select d;

                            if (!stockIn.Any())
                            {
                                var defaultPeriod = from d in posData3.MstPeriods select d;
                                var defaultSettings = from d in posData3.SysSettings select d;

                                var lastStockInNumber = from d in posData3.TrnStockIns.OrderByDescending(d => d.Id) select d;
                                var stockInNumberResult = defaultPeriod.FirstOrDefault().Period + "-000001";

                                if (lastStockInNumber.Any())
                                {
                                    var stockInNumberSplitStrings = lastStockInNumber.FirstOrDefault().StockInNumber;
                                    Int32 secondIndex = stockInNumberSplitStrings.IndexOf('-', stockInNumberSplitStrings.IndexOf('-'));
                                    var stockInNumberSplitStringValue = stockInNumberSplitStrings.Substring(secondIndex + 1);
                                    var stockInNumber = Convert.ToInt32(stockInNumberSplitStringValue) + 000001;
                                    stockInNumberResult = defaultPeriod.FirstOrDefault().Period + "-" + FillLeadingZeroes(stockInNumber, 6);
                                }

                                POSdb3.TrnStockIn newStockIn = new POSdb3.TrnStockIn
                                {
                                    PeriodId = defaultPeriod.FirstOrDefault().Id,
                                    StockInDate = Convert.ToDateTime(rr.RRDate),
                                    StockInNumber = stockInNumberResult,
                                    SupplierId = defaultSettings.FirstOrDefault().PostSupplierId,
                                    Remarks = fileName,
                                    IsReturn = false,
                                    CollectionId = null,
                                    PurchaseOrderId = null,
                                    PreparedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    CheckedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    ApprovedBy = defaultSettings.FirstOrDefault().PostUserId,
                                    IsLocked = 1,
                                    EntryUserId = defaultSettings.FirstOrDefault().PostUserId,
                                    EntryDateTime = DateTime.Now,
                                    UpdateUserId = defaultSettings.FirstOrDefault().PostUserId,
                                    UpdateDateTime = DateTime.Now,
                                    SalesId = null
                                };

                                posData3.TrnStockIns.InsertOnSubmit(newStockIn);
                                posData3.SubmitChanges();

                                foreach (var item in rr.ListPOSIntegrationTrnReceivingReceiptItem.ToList())
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
                                            POSdb3.TrnStockInLine newStockInLine = new POSdb3.TrnStockInLine
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

                                            posData3.TrnStockInLines.InsertOnSubmit(newStockInLine);

                                            var currentItem = from d in posData3.MstItems
                                                              where d.Id == newStockInLine.ItemId
                                                              select d;

                                            if (currentItem.Any())
                                            {
                                                Decimal currentOnHandQuantity = currentItem.FirstOrDefault().OnhandQuantity;
                                                Decimal totalQuantity = currentOnHandQuantity + Convert.ToDecimal(item.Quantity);

                                                var updateItem = currentItem.FirstOrDefault();
                                                updateItem.OnhandQuantity = totalQuantity;
                                            }

                                            posData3.SubmitChanges();
                                            Console.WriteLine("Receiving Receipt - " + fileName + " was successfully saved!");
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
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
