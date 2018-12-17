using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class TrnSalesReturnController
    {
        // ================
        // Get Sales Return
        // ================
        public void GetSalesReturn(String database, String apiUrlHost, String branchCode, String userCode)
        {
            try
            {
                var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                Data.POSDatabaseDataContext posData = new Data.POSDatabaseDataContext(newConnectionString);

                var discounts = from d in posData.MstDiscounts select d;
                if (discounts.Any())
                {
                    var taxes = from d in posData.MstTaxes select d;
                    if (taxes.Any())
                    {
                        var terms = from d in posData.MstTerms select d;
                        if (terms.Any())
                        {
                            var stockIns = from d in posData.TrnStockIns where d.IsReturn == true && d.CollectionId != null && d.SalesId != null && d.PostCode == null && d.IsLocked == true select d;
                            if (stockIns.Any())
                            {
                                var stockIn = stockIns.FirstOrDefault();

                                var stockInLines = from d in posData.TrnStockInLines where d.StockInId == stockIn.Id select d;
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
                                            NetPrice = stockInLine.Cost * -1,
                                            Amount = (stockInLine.Quantity * stockInLine.Cost) * -1,
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

                                    String json = new JavaScriptSerializer().Serialize(collectionData);

                                    Console.WriteLine("Sending Returned Sales: " + collectionData.DocumentReference);
                                    Console.WriteLine("Amount: " + collectionData.ListPOSIntegrationTrnSalesInvoiceItem.Sum(d => d.Amount).ToString("#,##0.00"));
                                    SendSalesReturn(database, apiUrlHost, json);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine();
            }
        }

        // =================
        // Send Sales Return
        // =================
        public void SendSalesReturn(String database, String apiUrlHost, String json)
        {
            try
            {
                // ============
                // Http Request
                // ============
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/add/POSIntegration/salesInvoice");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                // ====
                // Data
                // ====
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    TrnCollection collection = new JavaScriptSerializer().Deserialize<TrnCollection>(json);
                    streamWriter.Write(new JavaScriptSerializer().Serialize(collection));
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
                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        Data.POSDatabaseDataContext posData = new Data.POSDatabaseDataContext(newConnectionString);

                        TrnCollection collection = new JavaScriptSerializer().Deserialize<TrnCollection>(json);
                        var stockIns = from d in posData.TrnStockIns where d.StockInNumber.Equals(collection.DocumentReference) select d;
                        if (stockIns.Any())
                        {
                            var stockIn = stockIns.FirstOrDefault();
                            stockIn.PostCode = result.Replace("\"", "");
                            posData.SubmitChanges();
                        }

                        Console.WriteLine("Send Succesful!");
                        Console.WriteLine();
                    }
                }
            }
            catch (WebException we)
            {
                var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();

                Console.WriteLine(resp.Replace("\"", ""));
                Console.WriteLine();
            }
        }
    }
}