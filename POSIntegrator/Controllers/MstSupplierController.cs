using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class MstSupplierController
    {
        // ============
        // Data Context
        // ============
        private static Data.POSDatabaseDataContext posData;

        // ============
        // GET Supplier
        // ============
        public void GetSupplier(String database, String apiUrlHost)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/supplier/" + currentDate);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<POSIntegrator.MstSupplier> supplierLists = (List<POSIntegrator.MstSupplier>)js.Deserialize(result, typeof(List<POSIntegrator.MstSupplier>));

                    foreach (var supplierList in supplierLists)
                    {
                        var supplierData = new POSIntegrator.MstSupplier()
                        {
                            Article = supplierList.Article,
                            Address = supplierList.Address,
                            ContactNumber = supplierList.ContactNumber,
                            Term = supplierList.Term,
                            TaxNumber = supplierList.TaxNumber,
                        };

                        String jsonPath = "d:/innosoft/json/master";
                        String fileName = "supplier-" + supplierList.Article;

                        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                        {
                            fileName = fileName.Replace(c, '_');
                        }

                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(supplierData);
                        String jsonFileName = jsonPath + "\\" + fileName + ".json";

                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        posData = new Data.POSDatabaseDataContext(newConnectionString);

                        var suppliers = from d in posData.MstSuppliers
                                        where d.Supplier.Equals(supplierList.Article)
                                        select d;

                        if (suppliers.Any())
                        {
                            Boolean foundChanges = false;

                            if (!foundChanges)
                            {
                                if (!suppliers.FirstOrDefault().Supplier.Equals(supplierList.Article))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!suppliers.FirstOrDefault().Address.Equals(supplierList.Address))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!suppliers.FirstOrDefault().CellphoneNumber.Equals(supplierList.ContactNumber))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!suppliers.FirstOrDefault().MstTerm.Term.Equals(supplierList.Term))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!suppliers.FirstOrDefault().TIN.Equals(supplierList.TaxNumber))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (foundChanges)
                            {
                                File.WriteAllText(jsonFileName, json);
                                Console.WriteLine("Updating existing supplier...");
                                Console.WriteLine("Supplier: " + supplierList.Article);

                                UpdateSupplier(database);
                            }
                        }
                        else
                        {
                            var terms = from d in posData.MstTerms
                                        where d.Term.Equals(supplierList.Term)
                                        select d;

                            if (terms.Any())
                            {
                                File.WriteAllText(jsonFileName, json);
                                Console.WriteLine("Saving new supplier...");
                                Console.WriteLine("Supplier: " + supplierList.Article);

                                UpdateSupplier(database);
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

        // ===============
        // UPDATE Supplier
        // ===============
        public void UpdateSupplier(String database)
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
                    POSIntegrator.MstSupplier supplier = json_serializer.Deserialize<POSIntegrator.MstSupplier>(json);

                    var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                    posData = new Data.POSDatabaseDataContext(newConnectionString);

                    var accounts = from d in posData.MstAccounts
                                   select d;

                    if (accounts.Any())
                    {
                        String supplierTerm = supplier.Term;

                        var terms = from d in posData.MstTerms
                                    where d.Term.Equals(supplierTerm)
                                    select d;

                        if (terms.Any())
                        {
                            var suppliers = from d in posData.MstSuppliers
                                            where d.Supplier.Equals(supplier.Article)
                                            select d;

                            if (!suppliers.Any())
                            {
                                var defaultSettings = from d in posData.SysSettings select d;

                                Data.MstSupplier newSupplier = new Data.MstSupplier
                                {
                                    Supplier = supplier.Article,
                                    Address = supplier.Address,
                                    TelephoneNumber = "NA",
                                    CellphoneNumber = supplier.ContactNumber,
                                    FaxNumber = "NA",
                                    TermId = terms.FirstOrDefault().Id,
                                    TIN = supplier.TaxNumber,
                                    AccountId = 254,
                                    EntryUserId = defaultSettings.FirstOrDefault().PostUserId,
                                    EntryDateTime = DateTime.Now,
                                    UpdateUserId = defaultSettings.FirstOrDefault().PostUserId,
                                    UpdateDateTime = DateTime.Now,
                                    IsLocked = true,
                                };

                                posData.MstSuppliers.InsertOnSubmit(newSupplier);
                                posData.SubmitChanges();

                                Console.WriteLine("Save Successful!");
                                Console.WriteLine();

                                File.Delete(file);
                            }
                            else
                            {
                                var defaultSettings = from d in posData.SysSettings select d;

                                var updateSupplier = suppliers.FirstOrDefault();
                                updateSupplier.Supplier = supplier.Article;
                                updateSupplier.Address = supplier.Address;
                                updateSupplier.CellphoneNumber = supplier.ContactNumber;
                                updateSupplier.TermId = terms.FirstOrDefault().Id;
                                updateSupplier.TIN = supplier.TaxNumber;
                                updateSupplier.UpdateUserId = defaultSettings.FirstOrDefault().PostUserId;
                                updateSupplier.UpdateDateTime = DateTime.Now;
                                posData.SubmitChanges();

                                Console.WriteLine("Update Successful!");
                                Console.WriteLine();

                                File.Delete(file);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Save failed! Term mismatch.");
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
