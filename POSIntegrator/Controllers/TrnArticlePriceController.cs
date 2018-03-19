using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class TrnArticlePriceController
    {
        // ============
        // Data Context
        // ============
        private static Data.POSDatabaseDataContext posData;

        // ==============
        // GET Item Price
        // ==============
        public void GetItemPrice(String database, String apiUrlHost, String branchCode)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/itemPrice/" + branchCode + "/" + currentDate);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<POSIntegrator.TrnArticlePrice> itemPriceLists = (List<POSIntegrator.TrnArticlePrice>)js.Deserialize(result, typeof(List<POSIntegrator.TrnArticlePrice>));

                    foreach (var itemPriceList in itemPriceLists)
                    {
                        var itemPriceData = new POSIntegrator.TrnArticlePrice()
                        {
                            BranchCode = itemPriceList.BranchCode,
                            IPNumber = itemPriceList.IPNumber,
                            IPDate = itemPriceList.IPDate,
                            Particulars = itemPriceList.Particulars,
                            ManualIPNumber = itemPriceList.ManualIPNumber,
                            ItemCode = itemPriceList.ItemCode,
                            ItemDescription = itemPriceList.ItemDescription,
                            Price = itemPriceList.Price,
                            TriggerQuantity = itemPriceList.TriggerQuantity
                        };

                        String jsonPath = "d:/innosoft/json/IP";
                        String fileName = "IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.ItemCode + ")";

                        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                        {
                            fileName = fileName.Replace(c, '_');
                        }

                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(itemPriceData);
                        String jsonFileName = jsonPath + "\\" + fileName + ".json";

                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        posData = new Data.POSDatabaseDataContext(newConnectionString);

                        var item = from d in posData.MstItems
                                   where d.BarCode.Equals(itemPriceList.ItemCode)
                                   select d;

                        if (item.Any())
                        {
                            var itemPrices = from d in posData.MstItemPrices
                                             where d.ItemId == item.FirstOrDefault().Id
                                             select d;

                            if (itemPrices.Any())
                            {
                                var itemPrice = from d in itemPrices
                                                where d.PriceDescription.Equals("IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.IPDate + ")")
                                                select d;

                                if (itemPrice.Any())
                                {
                                    Boolean foundChanges = false;

                                    if (!foundChanges)
                                    {
                                        if (itemPrice.FirstOrDefault().Price != itemPriceList.Price)
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (itemPrice.FirstOrDefault().TriggerQuantity != itemPriceList.TriggerQuantity)
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (foundChanges)
                                    {
                                        File.WriteAllText(jsonFileName, json);
                                        Console.WriteLine("Updating existing Item Price...");
                                        Console.WriteLine("Barcode: " + itemPriceList.ItemCode);
                                        Console.WriteLine("Item: " + itemPriceList.ItemDescription);
                                        Console.WriteLine("Price Description: IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.IPDate + ")");

                                        UpdateItemPrice(database);
                                    }
                                }
                                else
                                {
                                    File.WriteAllText(jsonFileName, json);
                                    Console.WriteLine("Saving new Item Price...");
                                    Console.WriteLine("Barcode: " + itemPriceList.ItemCode);
                                    Console.WriteLine("Item: " + itemPriceList.ItemDescription);
                                    Console.WriteLine("Price Description: IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.IPDate + ")");

                                    UpdateItemPrice(database);
                                }
                            }
                            else
                            {
                                File.WriteAllText(jsonFileName, json);
                                Console.WriteLine("Saving new Item Price...");
                                Console.WriteLine("Barcode: " + itemPriceList.ItemCode);
                                Console.WriteLine("Item: " + itemPriceList.ItemDescription);
                                Console.WriteLine("Price Description: IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.IPDate + ")");

                                UpdateItemPrice(database);
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

        // =================
        // UPDATE Item Price
        // =================
        public void UpdateItemPrice(String database)
        {
            try
            {
                String jsonPath = "d:/innosoft/json/IP";
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
                    POSIntegrator.TrnArticlePrice itemPriceList = json_serializer.Deserialize<POSIntegrator.TrnArticlePrice>(json);

                    var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                    posData = new Data.POSDatabaseDataContext(newConnectionString);

                    var item = from d in posData.MstItems
                               where d.BarCode.Equals(itemPriceList.ItemCode)
                               select d;

                    if (item.Any())
                    {
                        var itemPrices = from d in posData.MstItemPrices
                                         where d.ItemId == item.FirstOrDefault().Id
                                         select d;

                        if (itemPrices.Any())
                        {
                            var itemPrice = from d in itemPrices
                                            where d.PriceDescription.Equals("IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.IPDate + ")")
                                            select d;

                            if (!itemPrice.Any())
                            {
                                Data.MstItemPrice newItemPrice = new Data.MstItemPrice
                                {
                                    ItemId = item.FirstOrDefault().Id,
                                    PriceDescription = "IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.IPDate + ")",
                                    Price = itemPriceList.Price,
                                    TriggerQuantity = itemPriceList.TriggerQuantity
                                };

                                posData.MstItemPrices.InsertOnSubmit(newItemPrice);
                                posData.SubmitChanges();

                                Console.WriteLine("Save Successful!");
                                Console.WriteLine();

                                File.Delete(file);
                            }
                            else
                            {
                                var updateItemPrice = itemPrices.FirstOrDefault();
                                updateItemPrice.Price = itemPriceList.Price;
                                updateItemPrice.TriggerQuantity = itemPriceList.TriggerQuantity;
                                posData.SubmitChanges();

                                Console.WriteLine("Update Successful!");
                                Console.WriteLine();

                                File.Delete(file);
                            }
                        }
                        else
                        {
                            Data.MstItemPrice newItemPrice = new Data.MstItemPrice
                            {
                                ItemId = item.FirstOrDefault().Id,
                                PriceDescription = "IP-" + itemPriceList.BranchCode + "-" + itemPriceList.IPNumber + " (" + itemPriceList.IPDate + ")",
                                Price = itemPriceList.Price,
                                TriggerQuantity = itemPriceList.TriggerQuantity
                            };

                            posData.MstItemPrices.InsertOnSubmit(newItemPrice);
                            posData.SubmitChanges();

                            Console.WriteLine("Save Successful!");
                            Console.WriteLine();

                            File.Delete(file);
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
