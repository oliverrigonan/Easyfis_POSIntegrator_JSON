﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApiContrib.Formatting;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator
{
    public class Collection
    {
        public string BranchCode { get; set; }
        public string CustomerManualArticleCode { get; set; }
        public string CreatedBy { get; set; }
        public string Term { get; set; }
        public string DocumentReference { get; set; }
        public string ManualSINumber { get; set; }
        public string Remarks { get; set; }
        public List<CollectionLines> listPOSIntegrationTrnSalesInvoiceItem { get; set; }
    }

    public class CollectionLines
    {
        public string ItemManualArticleCode { get; set; }
        public string Particulars { get; set; }
        public string Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public string Discount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetPrice { get; set; }
        public decimal Amount { get; set; }
        public string VAT { get; set; }
    }

    class Program
    {
        private static POSdbDataContext posData = new POSdbDataContext();

        public static void sendJsonFiles(string jsonPath)
        {
            try
            {
                List<string> files = new List<string>(Directory.EnumerateFiles(jsonPath));
                foreach (var file in files)
                {
                    // Read json file
                    string json;
                    using (StreamReader r = new StreamReader(file))
                    {
                        json = r.ReadToEnd();
                    }

                    // Send json to server
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.easyfis.com/api/add/POSIntegration/salesInvoice");
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";
                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        var json_serializer = new JavaScriptSerializer();
                        Collection c = json_serializer.Deserialize<Collection>(json);
                        streamWriter.Write(new JavaScriptSerializer().Serialize(c));
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    // Process response
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();

                        Console.WriteLine(result);

                        var json_serializer = new JavaScriptSerializer();
                        Collection c = json_serializer.Deserialize<Collection>(json);
                        var collections = from d in posData.TrnCollections
                                          where d.CollectionNumber == c.DocumentReference
                                          select d;

                        if (collections.Any())
                        {
                            var collection = collections.FirstOrDefault();
                            collection.PostCode = result.Replace("\"", "");
                            posData.SubmitChanges();
                            File.Delete(file);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Innosoft POS Uplader");
            Console.WriteLine("Version: 1.20170717 ");
            Console.WriteLine("====================");

            Console.Write("JSON Path:");
            string jsonPath = Console.ReadLine();

            while (true)
            {
                try
                {
                    var collections = from d in posData.TrnCollections where d.PostCode == null && d.CollectionNumber != "NA" select d;
                    if (collections.Any())
                    {
                        var sysSettings = from d in posData.SysSettings select d;
                        foreach (var collection in collections)
                        {
                            List<CollectionLines> listCollectionLines = new List<CollectionLines>();
                            foreach (var salesLine in collection.TrnSale.TrnSalesLines)
                            {
                                listCollectionLines.Add(new CollectionLines()
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
                                    VAT = salesLine.MstTax.Tax
                                });
                            }

                            var collectionData = new Collection()
                            {
                                BranchCode = sysSettings.FirstOrDefault().BranchCode,
                                CustomerManualArticleCode = collection.TrnSale.MstCustomer.CustomerCode,
                                CreatedBy = sysSettings.FirstOrDefault().UserCode,
                                Term = collection.TrnSale.MstTerm.Term,
                                DocumentReference = collection.CollectionNumber,
                                ManualSINumber = collection.TrnSale.SalesNumber,
                                Remarks = collection.Remarks != null ? collection.Remarks : "NA",
                                listPOSIntegrationTrnSalesInvoiceItem = listCollectionLines.ToList()
                            };

                            string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(collectionData);
                            string jsonFileName = jsonPath + "\\" + collection.CollectionNumber + ".json";
                            File.WriteAllText(jsonFileName, json);

                            Console.WriteLine("Saving " + collection.CollectionNumber + "...");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error...Retrying...");
                    }
                }
                catch
                {
                    Console.WriteLine("Error...Retrying...");
                }

                Thread.Sleep(5000);

                // Send Json Files
                sendJsonFiles(jsonPath);
            }
        }
    }
}