using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApiContrib.Formatting;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using System.Diagnostics;
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

        // ===================
        // Fill Leading Zeroes
        // ===================
        public static String FillLeadingZeroes(Int32 number, Int32 length)
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

        // ===============
        // Send Json Files
        // ===============
        public static void SendSIJsonFiles(String jsonPath, String apiUrlHost, String database)
        {
            try
            {
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

                    // ===================
                    // Send json to server
                    // ===================
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/add/POSIntegration/salesInvoice");
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        var json_serializer = new JavaScriptSerializer();
                        TrnCollection c = json_serializer.Deserialize<TrnCollection>(json);
                        streamWriter.Write(new JavaScriptSerializer().Serialize(c));
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    // ================
                    // Process response
                    // ================
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();

                        var json_serializer = new JavaScriptSerializer();
                        TrnCollection c = json_serializer.Deserialize<TrnCollection>(json);

                        Console.WriteLine("Collection Number " + c.DocumentReference + " was successfully sent!");
                        Console.WriteLine("Post Code: " + result.Replace("\"", ""));

                        if (database.Equals("1"))
                        {
                            var collections = from d in posData1.TrnCollections
                                              where d.CollectionNumber.Equals(c.DocumentReference)
                                              select d;

                            if (collections.Any())
                            {
                                var collection = collections.FirstOrDefault();
                                collection.PostCode = result.Replace("\"", "");
                                posData1.SubmitChanges();
                                File.Delete(file);
                            }
                        }
                        else
                        {
                            if (database.Equals("2"))
                            {
                                var collections = from d in posData2.TrnCollections
                                                  where d.CollectionNumber.Equals(c.DocumentReference)
                                                  select d;

                                if (collections.Any())
                                {
                                    var collection = collections.FirstOrDefault();
                                    collection.PostCode = result.Replace("\"", "");
                                    posData2.SubmitChanges();
                                    File.Delete(file);
                                }
                            }
                            else
                            {
                                if (database.Equals("3"))
                                {
                                    var collections = from d in posData3.TrnCollections
                                                      where d.CollectionNumber.Equals(c.DocumentReference)
                                                      select d;

                                    if (collections.Any())
                                    {
                                        var collection = collections.FirstOrDefault();
                                        collection.PostCode = result.Replace("\"", "");
                                        posData3.SubmitChanges();
                                        File.Delete(file);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Database not found!");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // =======================
        // GET Stock Transfer - IN
        // =======================
        public static void GetStockTransferIN(String database, String apiUrlHost, String stockTransferDate, String toBranchCode)
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/stockTransferItems/IN/" + stockTransferDate + "/" + toBranchCode);
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

                        String jsonPath = "d:/innosoft/json/IN";
                        String fileName = "ST-" + stockTransferList.BranchCode + "-" + stockTransferList.STNumber;

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
                                Console.WriteLine("Saving Stock Transfer (IN) - " + fileName + "...");
                                InsertStockTransferIN(database);
                            }
                            else
                            {
                                File.Delete(jsonFileName);
                            }
                        }
                        else
                        {
                            if (database.Equals("2"))
                            {
                                var stockIn = from d in posData2.TrnStockIns
                                              where d.Remarks.Equals(fileName)
                                              select d;

                                if (!stockIn.Any())
                                {
                                    Console.WriteLine("Saving Stock Transfer (IN) - " + fileName + "...");
                                    InsertStockTransferIN(database);
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
                                        Console.WriteLine("Saving Stock Transfer (IN) - " + fileName + "...");
                                        InsertStockTransferIN(database);
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // ==========================
        // INSERT Stock Transfer - IN 
        // ==========================
        public static void InsertStockTransferIN(String database)
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
                    TrnStockTransfer st = json_serializer.Deserialize<TrnStockTransfer>(json);

                    if (database.Equals("1"))
                    {
                        String fileName = "ST-" + st.BranchCode + "-" + st.STNumber;
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
                                StockInDate = Convert.ToDateTime(st.STDate),
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
                                        Console.WriteLine("Stock Transfer (IN) - " + fileName + " was successfully saved!");

                                        File.Delete(file);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (database.Equals("2"))
                        {
                            String fileName = "ST-" + st.BranchCode + "-" + st.STNumber;
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
                                    StockInDate = Convert.ToDateTime(st.STDate),
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
                                            Console.WriteLine("Stock Transfer (IN) - " + fileName + " was successfully saved!");

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
                                String fileName = "ST-" + st.BranchCode + "-" + st.STNumber;
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
                                        StockInDate = Convert.ToDateTime(st.STDate),
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
                                                Console.WriteLine("Stock Transfer (IN) - " + fileName + " was successfully saved!");

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

        // =======================
        // GET Stock Transfer - OT
        // =======================
        public static void GetStockTransferOT(String database, String apiUrlHost, String stockTransferDate, String fromBranchCode)
        {
            try
            {
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
                        else
                        {
                            if (database.Equals("2"))
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // ==========================
        // INSERT Stock Transfer - OT
        // ==========================
        public static void InsertStockTransferOT(String database)
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

                                            File.Delete(file);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (database.Equals("2"))
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // =============
        // GET Stock Out
        // =============
        public static void GetStockOut(String database, String apiUrlHost, String stockOutDate, String branchCode)
        {
            try
            {
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
                        else
                        {
                            if (database.Equals("2"))
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // ================
        // INSERT Stock Out
        // ================
        public static void InsertStockOut(String database)
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

                                            File.Delete(file);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (database.Equals("2"))
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // ===========
        // Main Method
        // ===========
        static void Main(String[] args)
        {
            Int32 i = 0;
            String apiUrlHost = "", database = "";
            foreach (var arg in args)
            {
                if (i == 0) { apiUrlHost = arg; }
                else if (i == 1) { database = arg; }
                i++;
            }

            Console.WriteLine("Innosoft POS Uploader");
            Console.WriteLine("Version: 1.20171107");
            Console.WriteLine("=====================");

            while (true)
            {
                String jsonPath = "d:/innosoft/json/SI";

                try
                {
                    if (database.Equals("1"))
                    {
                        // =====================
                        // Settings and Defaults
                        // =====================
                        var sysSettings = from d in posData1.SysSettings select d;
                        if (sysSettings.Any())
                        {
                            // =============
                            // Sales Invoice
                            // =============
                            var collections = from d in posData1.TrnCollections where d.PostCode == null && d.CollectionNumber != "NA" select d;
                            if (collections.Any())
                            {
                                foreach (var collection in collections)
                                {
                                    List<TrnCollectionLines> listCollectionLines = new List<TrnCollectionLines>();
                                    if (collection.TrnSale != null)
                                    {
                                        foreach (var salesLine in collection.TrnSale.TrnSalesLines)
                                        {
                                            listCollectionLines.Add(new TrnCollectionLines()
                                            {
                                                ItemManualArticleCode = salesLine.MstItem.BarCode,
                                                Particulars = salesLine.MstItem.ItemDescription,
                                                Unit = salesLine.MstUnit.Unit,
                                                Quantity = salesLine.Quantity,
                                                Price = salesLine.Price,
                                                Discount = salesLine.MstDiscount.Discount,
                                                DiscountAmount = salesLine.DiscountAmount,
                                                NetPrice = salesLine.NetPrice,
                                                Amount = salesLine.Amount,
                                                VAT = salesLine.MstTax.Tax,
                                                SalesItemTimeStamp = salesLine.SalesLineTimeStamp.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)
                                            });
                                        }

                                        var collectionData = new TrnCollection()
                                        {
                                            SIDate = collection.CollectionDate.ToShortDateString(),
                                            BranchCode = sysSettings.FirstOrDefault().BranchCode,
                                            CustomerManualArticleCode = collection.TrnSale.MstCustomer.CustomerCode,
                                            CreatedBy = sysSettings.FirstOrDefault().UserCode,
                                            Term = collection.TrnSale.MstTerm.Term,
                                            DocumentReference = collection.CollectionNumber,
                                            ManualSINumber = collection.TrnSale.SalesNumber,
                                            Remarks = collection.MstUser4.UserName,
                                            ListPOSIntegrationTrnSalesInvoiceItem = listCollectionLines.ToList()
                                        };

                                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(collectionData);
                                        String jsonFileName = jsonPath + "\\" + collection.CollectionNumber + ".json";
                                        File.WriteAllText(jsonFileName, json);

                                        Console.WriteLine("Sending Collection Number " + collection.CollectionNumber + "...");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Error... Retrying...");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error... Retrying...");
                            }

                            DateTime dateTimeToday = DateTime.Today;
                            var currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
                            var branchCode = sysSettings.FirstOrDefault().BranchCode;
                            GetStockTransferIN(database, apiUrlHost, currentDate, branchCode);
                            GetStockTransferOT(database, apiUrlHost, currentDate, branchCode);
                            GetStockOut(database, apiUrlHost, currentDate, branchCode);
                        }
                    }
                    else if (database.Equals("2"))
                    {
                        // =====================
                        // Settings and Defaults
                        // =====================
                        var sysSettings = from d in posData2.SysSettings select d;
                        if (sysSettings.Any())
                        {
                            // =============
                            // Sales Invoice
                            // =============
                            var collections = from d in posData2.TrnCollections where d.PostCode == null && d.CollectionNumber != "NA" select d;
                            if (collections.Any())
                            {
                                foreach (var collection in collections)
                                {
                                    List<TrnCollectionLines> listCollectionLines = new List<TrnCollectionLines>();
                                    if (collection.TrnSale != null)
                                    {
                                        foreach (var salesLine in collection.TrnSale.TrnSalesLines)
                                        {
                                            listCollectionLines.Add(new TrnCollectionLines()
                                            {
                                                ItemManualArticleCode = salesLine.MstItem.BarCode,
                                                Particulars = salesLine.MstItem.ItemDescription,
                                                Unit = salesLine.MstUnit.Unit,
                                                Quantity = salesLine.Quantity,
                                                Price = salesLine.Price,
                                                Discount = salesLine.MstDiscount.Discount,
                                                DiscountAmount = salesLine.DiscountAmount,
                                                NetPrice = salesLine.NetPrice,
                                                Amount = salesLine.Amount,
                                                VAT = salesLine.MstTax.Tax,
                                                SalesItemTimeStamp = salesLine.SalesLineTimeStamp.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)
                                            });
                                        }

                                        var collectionData = new TrnCollection()
                                        {
                                            SIDate = collection.CollectionDate.ToShortDateString(),
                                            BranchCode = sysSettings.FirstOrDefault().BranchCode,
                                            CustomerManualArticleCode = collection.TrnSale.MstCustomer.CustomerCode,
                                            CreatedBy = sysSettings.FirstOrDefault().UserCode,
                                            Term = collection.TrnSale.MstTerm.Term,
                                            DocumentReference = collection.CollectionNumber,
                                            ManualSINumber = collection.TrnSale.SalesNumber,
                                            Remarks = collection.MstUser4.UserName,
                                            ListPOSIntegrationTrnSalesInvoiceItem = listCollectionLines.ToList()
                                        };

                                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(collectionData);
                                        String jsonFileName = jsonPath + "\\" + collection.CollectionNumber + ".json";
                                        File.WriteAllText(jsonFileName, json);

                                        Console.WriteLine("Sending Collection Number " + collection.CollectionNumber + "...");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Error... Retrying...");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error... Retrying...");
                            }

                            DateTime dateTimeToday = DateTime.Today;
                            var currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
                            var branchCode = sysSettings.FirstOrDefault().BranchCode;
                            GetStockTransferIN(database, apiUrlHost, currentDate, branchCode);
                            GetStockTransferOT(database, apiUrlHost, currentDate, branchCode);
                            GetStockOut(database, apiUrlHost, currentDate, branchCode);
                        }
                    }
                    else if (database.Equals("3"))
                    {
                        // =====================
                        // Settings and Defaults
                        // =====================
                        var sysSettings = from d in posData3.SysSettings select d;
                        if (sysSettings.Any())
                        {
                            // =============
                            // Sales Invoice
                            // =============
                            var collections = from d in posData3.TrnCollections where d.PostCode == null && d.CollectionNumber != "NA" select d;
                            if (collections.Any())
                            {
                                foreach (var collection in collections)
                                {
                                    List<TrnCollectionLines> listCollectionLines = new List<TrnCollectionLines>();
                                    if (collection.TrnSale != null)
                                    {
                                        foreach (var salesLine in collection.TrnSale.TrnSalesLines)
                                        {
                                            listCollectionLines.Add(new TrnCollectionLines()
                                            {
                                                ItemManualArticleCode = salesLine.MstItem.BarCode,
                                                Particulars = salesLine.MstItem.ItemDescription,
                                                Unit = salesLine.MstUnit.Unit,
                                                Quantity = salesLine.Quantity,
                                                Price = salesLine.Price,
                                                Discount = salesLine.MstDiscount.Discount,
                                                DiscountAmount = salesLine.DiscountAmount,
                                                NetPrice = salesLine.NetPrice,
                                                Amount = salesLine.Amount,
                                                VAT = salesLine.MstTax.Tax,
                                                SalesItemTimeStamp = salesLine.SalesLineTimeStamp.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)
                                            });
                                        }

                                        var collectionData = new TrnCollection()
                                        {
                                            SIDate = collection.CollectionDate.ToShortDateString(),
                                            BranchCode = sysSettings.FirstOrDefault().BranchCode,
                                            CustomerManualArticleCode = collection.TrnSale.MstCustomer.CustomerCode,
                                            CreatedBy = sysSettings.FirstOrDefault().UserCode,
                                            Term = collection.TrnSale.MstTerm.Term,
                                            DocumentReference = collection.CollectionNumber,
                                            ManualSINumber = collection.TrnSale.SalesNumber,
                                            Remarks = collection.MstUser4.UserName,
                                            ListPOSIntegrationTrnSalesInvoiceItem = listCollectionLines.ToList()
                                        };

                                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(collectionData);
                                        String jsonFileName = jsonPath + "\\" + collection.CollectionNumber + ".json";
                                        File.WriteAllText(jsonFileName, json);

                                        Console.WriteLine("Sending Collection Number " + collection.CollectionNumber + "...");
                                    }
                                    else
                                    {
                                        Console.WriteLine("Error... Retrying...");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error... Retrying...");
                            }

                            DateTime dateTimeToday = DateTime.Today;
                            var currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
                            var branchCode = sysSettings.FirstOrDefault().BranchCode;
                            GetStockTransferIN(database, apiUrlHost, currentDate, branchCode);
                            GetStockTransferOT(database, apiUrlHost, currentDate, branchCode);
                            GetStockOut(database, apiUrlHost, currentDate, branchCode);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Database not found... Retrying...");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Thread.Sleep(5000);

                // ===============
                // Send Json Files
                // ===============
                SendSIJsonFiles(jsonPath, apiUrlHost, database);
            }
        }
    }
}
