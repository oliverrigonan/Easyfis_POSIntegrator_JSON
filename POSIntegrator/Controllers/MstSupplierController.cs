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
        // Get Supplier
        // ============
        public void GetSupplier(String database, String apiUrlHost)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                // ============
                // Http Request
                // ============
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/supplier/" + currentDate);
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
                    List<MstSupplier> supplierLists = (List<MstSupplier>)js.Deserialize(result, typeof(List<MstSupplier>));

                    if (supplierLists.Any())
                    {
                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        Data.POSDatabaseDataContext posData = new Data.POSDatabaseDataContext(newConnectionString);

                        foreach (var supplier in supplierLists)
                        {
                            var terms = from d in posData.MstTerms where d.Term.Equals(supplier.Term) select d;
                            if (terms.Any())
                            {
                                var defaultSettings = from d in posData.SysSettings select d;

                                var currentSupplier = from d in posData.MstSuppliers where d.Supplier.Equals(supplier.Article) select d;
                                if (currentSupplier.Any())
                                {
                                    Boolean foundChanges = false;

                                    if (!foundChanges)
                                    {
                                        if (!currentSupplier.FirstOrDefault().Supplier.Equals(supplier.Article))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (!currentSupplier.FirstOrDefault().Address.Equals(supplier.Address))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (!currentSupplier.FirstOrDefault().CellphoneNumber.Equals(supplier.ContactNumber))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (!currentSupplier.FirstOrDefault().MstTerm.Term.Equals(supplier.Term))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (!currentSupplier.FirstOrDefault().TIN.Equals(supplier.TaxNumber))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (foundChanges)
                                    {
                                        Console.WriteLine("Updating Supplier: " + currentSupplier.FirstOrDefault().Supplier);
                                        Console.WriteLine("Contact No.: " + currentSupplier.FirstOrDefault().CellphoneNumber);

                                        var updateSupplier = currentSupplier.FirstOrDefault();
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
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Saving Supplier: " + supplier.Article);
                                    Console.WriteLine("Contact No.: " + supplier.ContactNumber);

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
                                }
                            }
                            else
                            {
                                Console.WriteLine("Cannot Save Supplier: " + supplier.Article);
                                Console.WriteLine("Term Mismatch!");
                                Console.WriteLine("Save Failed!");
                                Console.WriteLine();
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
    }
}