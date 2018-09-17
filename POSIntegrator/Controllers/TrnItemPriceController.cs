using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class TrnItemPriceController
    {
        // ==============
        // Get Item Price
        // ==============
        public void GetItemPrice(String database, String apiUrlHost, String branchCode)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                // ============
                // Http Request
                // ============
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/itemPrice/" + branchCode + "/" + currentDate);
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
                    List<TrnArticlePrice> itemPriceLists = (List<POSIntegrator.TrnArticlePrice>)js.Deserialize(result, typeof(List<TrnArticlePrice>));

                    if (itemPriceLists.Any())
                    {
                        foreach (var itemPrice in itemPriceLists)
                        {
                            var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                            Data.POSDatabaseDataContext posData = new Data.POSDatabaseDataContext(newConnectionString);

                            var item = from d in posData.MstItems where d.BarCode.Equals(itemPrice.ItemCode) select d;
                            if (item.Any())
                            {
                                var currentItemPrice = from d in posData.MstItemPrices where d.ItemId == item.FirstOrDefault().Id select d;
                                if (currentItemPrice.Any())
                                {
                                    Boolean foundChanges = false;

                                    if (!foundChanges)
                                    {
                                        if (!currentItemPrice.FirstOrDefault().PriceDescription.Equals("IP-" + itemPrice.BranchCode + "-" + itemPrice.IPNumber + " (" + itemPrice.IPDate + ")"))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (currentItemPrice.FirstOrDefault().Price != itemPrice.Price)
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (currentItemPrice.FirstOrDefault().TriggerQuantity != itemPrice.TriggerQuantity)
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (foundChanges)
                                    {
                                        Console.WriteLine("Updating Item Price: " + currentItemPrice.FirstOrDefault().PriceDescription);
                                        Console.WriteLine("Current Item: " + item.FirstOrDefault().ItemDescription);

                                        var updateItemPrice = currentItemPrice.FirstOrDefault();
                                        updateItemPrice.PriceDescription = "IP-" + itemPrice.BranchCode + "-" + itemPrice.IPNumber + " (" + itemPrice.IPDate + ")";
                                        updateItemPrice.Price = itemPrice.Price;
                                        updateItemPrice.TriggerQuantity = itemPrice.TriggerQuantity;
                                        posData.SubmitChanges();

                                        Console.WriteLine("Update Successful!");
                                        Console.WriteLine();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Saving Item Price: IP-" + itemPrice.BranchCode + "-" + itemPrice.IPNumber + " (" + itemPrice.IPDate + ")");
                                    Console.WriteLine("Current Item: " + item.FirstOrDefault().ItemDescription);

                                    Data.MstItemPrice newPrice = new Data.MstItemPrice()
                                    {
                                        ItemId = item.FirstOrDefault().Id,
                                        PriceDescription = "IP-" + itemPrice.BranchCode + "-" + itemPrice.IPNumber + " (" + itemPrice.IPDate + ")",
                                        Price = itemPrice.Price,
                                        TriggerQuantity = itemPrice.TriggerQuantity
                                    };

                                    posData.MstItemPrices.InsertOnSubmit(newPrice);
                                    posData.SubmitChanges();

                                    Console.WriteLine("Save Successful!");
                                    Console.WriteLine();
                                }
                            }
                            else
                            {
                                Console.WriteLine("Cannot Save Item Price: IP-" + itemPrice.BranchCode + "-" + itemPrice.IPNumber + " (" + itemPrice.IPDate + ")" + "...");
                                Console.WriteLine("Item Not Found!");
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