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

        // ========
        // GET Item
        // ========
        public void GetItem(String database, String apiUrlHost)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/item/" + currentDate);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<POSIntegrator.MstItem> itemLists = (List<POSIntegrator.MstItem>)js.Deserialize(result, typeof(List<POSIntegrator.MstItem>));

                    foreach (var itemList in itemLists)
                    {
                        List<MstItemPrice> listItemPrice = new List<MstItemPrice>();
                        foreach (var itemPriceList in itemList.ListItemPrice)
                        {
                            listItemPrice.Add(new MstItemPrice()
                            {
                                ArticleId = itemPriceList.ArticleId,
                                PriceDescription = itemPriceList.PriceDescription,
                                Price = itemPriceList.Price,
                                Remarks = itemPriceList.Remarks,
                            });
                        }

                        var itemData = new POSIntegrator.MstItem()
                        {
                            ManualArticleCode = itemList.ManualArticleCode,
                            Article = itemList.Article,
                            Category = itemList.Category,
                            Unit = itemList.Unit,
                            Price = itemList.Price,
                            Cost = itemList.Cost,
                            IsInventory = itemList.IsInventory,
                            Particulars = itemList.Particulars,
                            OutputTax = itemList.OutputTax,
                            ListItemPrice = itemList.ListItemPrice.ToList()
                        };

                        String jsonPath = "d:/innosoft/json/master";
                        String fileName = "item-" + itemList.ManualArticleCode;

                        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                        {
                            fileName = fileName.Replace(c, '_');
                        }

                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(itemData);
                        String jsonFileName = jsonPath + "\\" + fileName + ".json";

                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        posData = new Data.POSDatabaseDataContext(newConnectionString);

                        var items = from d in posData.MstItems
                                    where d.BarCode.Equals(itemList.ManualArticleCode)
                                    select d;

                        if (items.Any())
                        {
                            Boolean foundChanges = false;

                            if (!foundChanges)
                            {
                                if (!items.FirstOrDefault().BarCode.Equals(itemList.ManualArticleCode))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!items.FirstOrDefault().ItemDescription.Equals(itemList.Article))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!items.FirstOrDefault().Category.Equals(itemList.Category))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!items.FirstOrDefault().MstUnit.Unit.Equals(itemList.Unit))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (items.FirstOrDefault().Price != itemList.Price)
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (items.FirstOrDefault().Cost != itemList.Cost)
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (items.FirstOrDefault().IsInventory != itemList.IsInventory)
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (items.FirstOrDefault().Remarks != null)
                                {
                                    if (!items.FirstOrDefault().Remarks.Equals(itemList.Particulars))
                                    {
                                        foundChanges = true;
                                    }
                                }
                            }

                            if (!foundChanges)
                            {
                                var taxes = from d in posData.MstTaxes
                                            where d.Tax.Equals(itemList.OutputTax)
                                            select d;

                                if (taxes.Any())
                                {
                                    if (items.FirstOrDefault().OutTaxId != taxes.FirstOrDefault().Id)
                                    {
                                        foundChanges = true;
                                    }
                                }
                            }

                            if (!foundChanges)
                            {
                                if (itemList.ListItemPrice.Any())
                                {
                                    var posItemPrices = from d in posData.MstItemPrices
                                                        where d.MstItem.BarCode.Equals(itemList.ManualArticleCode)
                                                        select d;

                                    if (posItemPrices.Any())
                                    {
                                        int posItemPriceCount = posItemPrices.Count();
                                        int itemPriceListCount = itemList.ListItemPrice.Count();

                                        if (posItemPriceCount != itemPriceListCount)
                                        {
                                            foundChanges = true;
                                        }
                                        else
                                        {
                                            foreach (var itemPrice in itemList.ListItemPrice.ToList())
                                            {
                                                var currentPOSItemPrices = from d in posItemPrices
                                                                           where d.PriceDescription.Equals(itemPrice.PriceDescription)
                                                                           && d.Price == itemPrice.Price
                                                                           select d;

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
                                File.WriteAllText(jsonFileName, json);
                                Console.WriteLine("Updating Item...");

                                UpdateItem(database);
                            }
                        }
                        else
                        {
                            var units = from d in posData.MstUnits
                                        where d.Unit.Equals(itemList.Unit)
                                        select d;

                            if (units.Any())
                            {
                                var taxes = from d in posData.MstTaxes
                                            where d.Tax.Equals(itemList.OutputTax)
                                            select d;

                                if (taxes.Any())
                                {
                                    File.WriteAllText(jsonFileName, json);
                                    Console.WriteLine("Saving Item...");

                                    UpdateItem(database);
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
        // UPDATE Item
        // ===========
        public void UpdateItem(String database)
        {
            try
            {
                String jsonPath = "d:/innosoft/json/master";
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
                    POSIntegrator.MstItem item = json_serializer.Deserialize<POSIntegrator.MstItem>(json);

                    var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                    posData = new Data.POSDatabaseDataContext(newConnectionString);

                    var accounts = from d in posData.MstAccounts
                                   select d;

                    if (accounts.Any())
                    {
                        String itemUnit = item.Unit;
                        String itemOutputTax = item.OutputTax;

                        var units = from d in posData.MstUnits
                                    where d.Unit.Equals(itemUnit)
                                    select d;

                        if (units.Any())
                        {
                            var taxes = from d in posData.MstTaxes
                                        where d.Tax.Equals(itemOutputTax)
                                        select d;

                            if (taxes.Any())
                            {
                                var supplier = from d in posData.MstSuppliers
                                               select d;

                                if (supplier.Any())
                                {
                                    var items = from d in posData.MstItems
                                                where d.BarCode.Equals(item.ManualArticleCode)
                                                select d;

                                    if (!items.Any())
                                    {
                                        var defaultSettings = from d in posData.SysSettings select d;

                                        var defaultItemCode = "000001";
                                        var lastItem = from d in posData.MstItems.OrderByDescending(d => d.Id)
                                                       select d;

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
                                            Alias = "NA",
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

                                        Console.WriteLine("Barcode: " + item.ManualArticleCode);
                                        Console.WriteLine("Item: " + item.Article);
                                        Console.WriteLine("Save Successful!");
                                        Console.WriteLine();

                                        File.Delete(file);
                                    }
                                    else
                                    {
                                        var defaultSettings = from d in posData.SysSettings select d;

                                        var updateItem = items.FirstOrDefault();
                                        updateItem.BarCode = item.ManualArticleCode;
                                        updateItem.ItemDescription = item.Article;
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
                                            var posItemPrices = from d in posData.MstItemPrices
                                                                where d.ItemId == items.FirstOrDefault().Id
                                                                select d;

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
                                                        ItemId = items.FirstOrDefault().Id,
                                                        PriceDescription = itemPrice.PriceDescription,
                                                        Price = itemPrice.Price,
                                                        TriggerQuantity = 0
                                                    };

                                                    posData.MstItemPrices.InsertOnSubmit(newItemPrice);
                                                }

                                                posData.SubmitChanges();
                                            }
                                        }

                                        Console.WriteLine("Barcode: " + item.ManualArticleCode);
                                        Console.WriteLine("Item: " + item.Article);
                                        Console.WriteLine("Update Successful!");
                                        Console.WriteLine();

                                        File.Delete(file);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Barcode: " + item.ManualArticleCode);
                                Console.WriteLine("Item: " + item.Article);
                                Console.WriteLine("Save Failed! Output Tax Mismatch!");
                                Console.WriteLine();

                                File.Delete(file);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Barcode: " + item.ManualArticleCode);
                            Console.WriteLine("Item: " + item.Article);
                            Console.WriteLine("Save Failed! Unit Mismatch!");
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
