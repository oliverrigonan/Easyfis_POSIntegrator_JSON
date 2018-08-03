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

        // ==============================
        // CREATE Stock In (Sales Return)
        // ==============================
        public void CreateStockInSalesReturn(String database, String apiUrlHost, String branchCode, String userCode)
        {
            try
            {
                String jsonPath = "d:/innosoft/json/return";

                var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                posData = new Data.POSDatabaseDataContext(newConnectionString);

                var discounts = from d in posData.MstDiscounts
                                select d;

                if (discounts.Any())
                {
                    var taxes = from d in posData.MstTaxes
                                select d;

                    if (taxes.Any())
                    {
                        var terms = from d in posData.MstTerms
                                    select d;

                        if (terms.Any())
                        {
                            var stockIns = from d in posData.TrnStockIns
                                           where d.IsReturn == true
                                           && d.CollectionId != null
                                           && d.PostCode == null
                                           && d.IsLocked == true
                                           select d;

                            if (stockIns.Any())
                            {
                                foreach (var stockIn in stockIns)
                                {
                                    var stockInLines = from d in posData.TrnStockInLines
                                                       where d.StockInId == stockIn.Id
                                                       select d;

                                    if (stockInLines.Any())
                                    {
                                        List<TrnCollectionLines> listCollectionLines = new List<TrnCollectionLines>();
                                        foreach (var stockInLine in stockInLines)
                                        {
                                            listCollectionLines.Add(new TrnCollectionLines()
                                            {
                                                ItemManualArticleCode = stockInLine.MstItem.BarCode,
                                                Particulars = stockInLine.MstItem.ItemDescription,
                                                Unit = stockInLine.MstUnit.Unit,
                                                Quantity = stockInLine.Quantity * -1,
                                                Price = stockInLine.Cost * -1,
                                                Discount = discounts.FirstOrDefault().Discount,
                                                DiscountAmount = 0,
                                                NetPrice = (stockInLine.Cost * -1),
                                                Amount = ((stockInLine.Quantity * -1) * (stockInLine.Cost * -1)) * -1,
                                                VAT = taxes.FirstOrDefault().Tax,
                                                SalesItemTimeStamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)
                                            });
                                        }

                                        var collectionData = new POSIntegrator.TrnCollection()
                                        {
                                            SIDate = stockIn.StockInDate.ToShortDateString(),
                                            BranchCode = branchCode,
                                            CustomerManualArticleCode = stockIn.TrnCollection.TrnSale.MstCustomer.CustomerCode,
                                            CreatedBy = userCode,
                                            Term = terms.FirstOrDefault().Term,
                                            DocumentReference = stockIn.StockInNumber,
                                            ManualSINumber = stockIn.TrnCollection.TrnSale.SalesNumber,
                                            Remarks = "Return from Customer",
                                            ListPOSIntegrationTrnSalesInvoiceItem = listCollectionLines.ToList()
                                        };

                                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(collectionData);
                                        String jsonFileName = jsonPath + "\\" + stockIn.StockInNumber + ".json";

                                        if (!File.Exists(jsonFileName))
                                        {
                                            File.WriteAllText(jsonFileName, json);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                SendStockInSalesReturn(database, apiUrlHost);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // ============================
        // SEND Stock In (Sales Return)
        // ============================
        public void SendStockInSalesReturn(String database, String apiUrlHost)
        {
            try
            {
                String jsonPath = "d:/innosoft/json/return";
                List<String> files = new List<String>(Directory.EnumerateFiles(jsonPath));

                if (files.Any())
                {
                    var file = files.FirstOrDefault();

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
                        POSIntegrator.TrnCollection c = json_serializer.Deserialize<POSIntegrator.TrnCollection>(json);

                        Console.WriteLine("Sending Collection...");
                        streamWriter.Write(new JavaScriptSerializer().Serialize(c));
                    }

                    // ================
                    // Process response
                    // ================
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        if (result != null)
                        {
                            var json_serializer = new JavaScriptSerializer();
                            POSIntegrator.TrnCollection c = json_serializer.Deserialize<POSIntegrator.TrnCollection>(json);

                            Console.WriteLine("Collection No.: " + c.DocumentReference);
                            Console.WriteLine("Customer Code: " + c.CustomerManualArticleCode);
                            Console.WriteLine("Sales No.: " + c.ManualSINumber);
                            Console.WriteLine("Remarks: " + c.Remarks);
                            Console.WriteLine("Post Code: " + result.Replace("\"", ""));
                            Console.WriteLine("Sent Succesful!");
                            Console.WriteLine();

                            var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                            posData = new Data.POSDatabaseDataContext(newConnectionString);

                            var stockIns = from d in posData.TrnStockIns
                                           where d.StockInNumber.Equals(c.DocumentReference)
                                           select d;

                            if (stockIns.Any())
                            {
                                var stockIn = stockIns.FirstOrDefault();
                                stockIn.PostCode = result.Replace("\"", "");
                                posData.SubmitChanges();

                                File.Delete(file);
                            }
                        }
                    }
                }
            }
            catch (WebException we)
            {
                var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();

                String jsonPath = "d:/innosoft/json/SI";
                List<String> files = new List<String>(Directory.EnumerateFiles(jsonPath));
                if (files.Any())
                {
                    var file = files.FirstOrDefault();

                    String json;
                    using (StreamReader r = new StreamReader(file))
                    {
                        json = r.ReadToEnd();
                    }

                    var json_serializer = new JavaScriptSerializer();
                    POSIntegrator.TrnCollection c = json_serializer.Deserialize<POSIntegrator.TrnCollection>(json);

                    Console.WriteLine("Collection No.: " + c.DocumentReference);
                    Console.WriteLine("Customer Code: " + c.CustomerManualArticleCode);
                    Console.WriteLine("Sales No.: " + c.ManualSINumber);
                    Console.WriteLine("Remarks: " + c.Remarks);
                    Console.WriteLine(resp.Replace("\"", ""));
                    Console.WriteLine();
                }
            }
        }
    }
}
