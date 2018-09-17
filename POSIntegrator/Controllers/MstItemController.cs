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
    class MstItemController
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

        // ========
        // Get Item
        // ========
        public void GetItem(String database, String apiUrlHost)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                // ============
                // Http Request
                // ============
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/item/" + currentDate);
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
                    List<MstItem> itemLists = (List<MstItem>)js.Deserialize(result, typeof(List<MstItem>));

                    if (itemLists.Any())
                    {
                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        Data.POSDatabaseDataContext posData = new Data.POSDatabaseDataContext(newConnectionString);

                        foreach (var item in itemLists)
                        {
                            var units = from d in posData.MstUnits where d.Unit.Equals(item.Unit) select d;
                            if (units.Any())
                            {
                                var taxes = from d in posData.MstTaxes where d.Tax.Equals(item.OutputTax) select d;
                                if (taxes.Any())
                                {
                                    var supplier = from d in posData.MstSuppliers select d;
                                    if (supplier.Any())
                                    {
                                        var defaultSettings = from d in posData.SysSettings select d;

                                        var currentItem = from d in posData.MstItems where d.BarCode.Equals(item.ManualArticleCode) select d;
                                        if (currentItem.Any())
                                        {
                                            Boolean foundChanges = false;

                                            if (!foundChanges)
                                            {
                                                if (!currentItem.FirstOrDefault().BarCode.Equals(item.ManualArticleCode))
                                                {
                                                    foundChanges = true;
                                                }
                                            }

                                            if (!foundChanges)
                                            {
                                                if (!currentItem.FirstOrDefault().ItemDescription.Equals(item.Article))
                                                {
                                                    foundChanges = true;
                                                }
                                            }

                                            if (!foundChanges)
                                            {
                                                if (!currentItem.FirstOrDefault().Category.Equals(item.Category))
                                                {
                                                    foundChanges = true;
                                                }
                                            }

                                            if (!foundChanges)
                                            {
                                                if (!currentItem.FirstOrDefault().MstUnit.Unit.Equals(item.Unit))
                                                {
                                                    foundChanges = true;
                                                }
                                            }

                                            if (!foundChanges)
                                            {
                                                if (currentItem.FirstOrDefault().Price != item.Price)
                                                {
                                                    foundChanges = true;
                                                }
                                            }

                                            if (!foundChanges)
                                            {
                                                if (currentItem.FirstOrDefault().Cost != item.Cost)
                                                {
                                                    foundChanges = true;
                                                }
                                            }

                                            if (!foundChanges)
                                            {
                                                if (currentItem.FirstOrDefault().IsInventory != item.IsInventory)
                                                {
                                                    foundChanges = true;
                                                }
                                            }

                                            if (!foundChanges)
                                            {
                                                if (currentItem.FirstOrDefault().Remarks != null)
                                                {
                                                    if (!currentItem.FirstOrDefault().Remarks.Equals(item.Particulars))
                                                    {
                                                        foundChanges = true;
                                                    }
                                                }
                                            }

                                            if (!foundChanges)
                                            {
                                                if (currentItem.FirstOrDefault().OutTaxId != taxes.FirstOrDefault().Id)
                                                {
                                                    foundChanges = true;
                                                }
                                            }

                                            if (!foundChanges)
                                            {
                                                if (item.ListItemPrice.Any())
                                                {
                                                    var posItemPrices = from d in posData.MstItemPrices where d.MstItem.BarCode.Equals(item.ManualArticleCode) select d;
                                                    if (posItemPrices.Any())
                                                    {
                                                        int posItemPriceCount = posItemPrices.Count();
                                                        int itemPriceListCount = item.ListItemPrice.Count();

                                                        if (posItemPriceCount != itemPriceListCount)
                                                        {
                                                            foundChanges = true;
                                                        }
                                                        else
                                                        {
                                                            foreach (var itemPrice in item.ListItemPrice.ToList())
                                                            {
                                                                var currentPOSItemPrices = from d in posItemPrices where d.PriceDescription.Equals(itemPrice.PriceDescription) && d.Price == itemPrice.Price select d;
                                                                if (!currentPOSItemPrices.Any())
                                                                {
                                                                    foundChanges = true;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            if (foundChanges)
                                            {
                                                Console.WriteLine("Updating Item: " + currentItem.FirstOrDefault().ItemDescription);

                                                var updateItem = currentItem.FirstOrDefault();
                                                updateItem.BarCode = item.ManualArticleCode;
                                                updateItem.ItemDescription = item.Article;
                                                updateItem.Alias = item.Article;
                                                updateItem.GenericName = item.Article;
                                                updateItem.Category = item.Category;
                                                updateItem.UnitId = units.FirstOrDefault().Id;
                                                updateItem.Price = item.Price;
                                                updateItem.Cost = item.Cost;
                                                updateItem.IsInventory = item.IsInventory;
                                                updateItem.Remarks = item.Particulars;
                                                updateItem.OutTaxId = taxes.FirstOrDefault().Id;
                                                updateItem.UpdateUserId = defaultSettings.FirstOrDefault().PostUserId;
                                                updateItem.UpdateDateTime = DateTime.Now;
                                                posData.SubmitChanges();

                                                if (item.ListItemPrice.Any())
                                                {
                                                    var posItemPrices = from d in posData.MstItemPrices where d.ItemId == currentItem.FirstOrDefault().Id select d;

                                                    bool isEmpty = false;
                                                    if (posItemPrices.Any())
                                                    {
                                                        posData.MstItemPrices.DeleteAllOnSubmit(posItemPrices);
                                                        posData.SubmitChanges();

                                                        isEmpty = true;
                                                    }

                                                    if (isEmpty)
                                                    {
                                                        foreach (var itemPrice in item.ListItemPrice.ToList())
                                                        {
                                                            Data.MstItemPrice newItemPrice = new Data.MstItemPrice
                                                            {
                                                                ItemId = currentItem.FirstOrDefault().Id,
                                                                PriceDescription = itemPrice.PriceDescription,
                                                                Price = itemPrice.Price,
                                                                TriggerQuantity = 0
                                                            };

                                                            posData.MstItemPrices.InsertOnSubmit(newItemPrice);
                                                        }

                                                        posData.SubmitChanges();
                                                    }
                                                }

                                                Console.WriteLine("Update Successful!");
                                                Console.WriteLine();
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("Saving Item: " + item.Article);

                                            var defaultItemCode = "000001";
                                            var lastItem = from d in posData.MstItems.OrderByDescending(d => d.Id) select d;
                                            if (lastItem.Any())
                                            {
                                                var OTNumber = Convert.ToInt32(lastItem.FirstOrDefault().ItemCode) + 000001;
                                                defaultItemCode = FillLeadingZeroes(OTNumber, 6);
                                            }

                                            Data.MstItem newItem = new Data.MstItem
                                            {
                                                ItemCode = defaultItemCode,
                                                BarCode = item.ManualArticleCode,
                                                ItemDescription = item.Article,
                                                Alias = item.Article,
                                                GenericName = item.Article,
                                                Category = item.Category,
                                                SalesAccountId = 159,
                                                AssetAccountId = 74,
                                                CostAccountId = 238,
                                                InTaxId = 4,
                                                OutTaxId = taxes.FirstOrDefault().Id,
                                                UnitId = units.FirstOrDefault().Id,
                                                DefaultSupplierId = supplier.FirstOrDefault().Id,
                                                Cost = item.Cost,
                                                MarkUp = 0,
                                                Price = item.Price,
                                                ImagePath = "NA",
                                                ReorderQuantity = 0,
                                                OnhandQuantity = 0,
                                                IsInventory = item.IsInventory,
                                                ExpiryDate = null,
                                                LotNumber = " ",
                                                Remarks = item.Particulars,
                                                EntryUserId = defaultSettings.FirstOrDefault().PostUserId,
                                                EntryDateTime = DateTime.Now,
                                                UpdateUserId = defaultSettings.FirstOrDefault().PostUserId,
                                                UpdateDateTime = DateTime.Now,
                                                IsLocked = true,
                                                DefaultKitchenReport = " ",
                                                IsPackage = false
                                            };

                                            posData.MstItems.InsertOnSubmit(newItem);
                                            posData.SubmitChanges();

                                            if (item.ListItemPrice.Any())
                                            {
                                                foreach (var itemPrice in item.ListItemPrice.ToList())
                                                {
                                                    Data.MstItemPrice newItemPrice = new Data.MstItemPrice
                                                    {
                                                        ItemId = newItem.Id,
                                                        PriceDescription = itemPrice.PriceDescription,
                                                        Price = itemPrice.Price,
                                                        TriggerQuantity = 0
                                                    };

                                                    posData.MstItemPrices.InsertOnSubmit(newItemPrice);
                                                }

                                                posData.SubmitChanges();
                                            }

                                            Console.WriteLine("Save Successful!");
                                            Console.WriteLine();
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Cannot Save Item: " + item.Article);
                                        Console.WriteLine("Empty Supplier!");
                                        Console.WriteLine();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Cannot Save Item: " + item.Article);
                                    Console.WriteLine("Output Tax Mismatch!");
                                    Console.WriteLine();
                                }
                            }
                            else
                            {
                                Console.WriteLine("Cannot Save Item: " + item.Article);
                                Console.WriteLine("Unit Mismatch!");
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