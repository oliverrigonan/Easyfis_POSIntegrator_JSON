using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace POSIntegrator.Controllers
{
    class MstCustomerController
    {
        // ============
        // Get Customer
        // ============
        public void GetCustomer(String database, String apiUrlHost)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                // ============
                // Http Request
                // ============
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/customer/" + currentDate);
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
                    List<MstCustomer> customerLists = (List<MstCustomer>)js.Deserialize(result, typeof(List<MstCustomer>));

                    if (customerLists.Any())
                    {
                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        Data.POSDatabaseDataContext posData = new Data.POSDatabaseDataContext(newConnectionString);

                        foreach (var customer in customerLists)
                        {
                            var terms = from d in posData.MstTerms where d.Term.Equals(customer.Term) select d;
                            if (terms.Any())
                            {
                                var defaultSettings = from d in posData.SysSettings select d;

                                var currentCustomer = from d in posData.MstCustomers where d.CustomerCode.Equals(customer.ManualArticleCode) && d.CustomerCode != null select d;
                                if (currentCustomer.Any())
                                {
                                    Boolean foundChanges = false;

                                    if (!foundChanges)
                                    {
                                        if (!currentCustomer.FirstOrDefault().Customer.Equals(customer.Article))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (!currentCustomer.FirstOrDefault().Address.Equals(customer.Address))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (!currentCustomer.FirstOrDefault().ContactPerson.Equals(customer.ContactPerson))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (!currentCustomer.FirstOrDefault().ContactNumber.Equals(customer.ContactNumber))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (!currentCustomer.FirstOrDefault().MstTerm.Term.Equals(customer.Term))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (!currentCustomer.FirstOrDefault().TIN.Equals(customer.TaxNumber))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (!foundChanges)
                                    {
                                        if (Convert.ToDecimal(currentCustomer.FirstOrDefault().CreditLimit) != Convert.ToDecimal(customer.CreditLimit))
                                        {
                                            foundChanges = true;
                                        }
                                    }

                                    if (foundChanges)
                                    {
                                        Console.WriteLine("Updating Customer: " + currentCustomer.FirstOrDefault().Customer);
                                        Console.WriteLine("Customer Code: " + currentCustomer.FirstOrDefault().CustomerCode);

                                        var updateCustomer = currentCustomer.FirstOrDefault();
                                        updateCustomer.Customer = customer.Article;
                                        updateCustomer.Address = customer.Address;
                                        updateCustomer.ContactPerson = customer.ContactPerson;
                                        updateCustomer.ContactNumber = customer.ContactNumber;
                                        updateCustomer.CreditLimit = customer.CreditLimit;
                                        updateCustomer.TermId = terms.FirstOrDefault().Id;
                                        updateCustomer.TIN = customer.TaxNumber;
                                        updateCustomer.UpdateUserId = defaultSettings.FirstOrDefault().PostUserId;
                                        updateCustomer.UpdateDateTime = DateTime.Now;
                                        updateCustomer.CustomerCode = customer.ManualArticleCode;
                                        posData.SubmitChanges();

                                        Console.WriteLine("Update Successful!");
                                        Console.WriteLine();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Saving Customer: " + customer.Article);
                                    Console.WriteLine("Customer Code: " + customer.ManualArticleCode);

                                    Data.MstCustomer newCustomer = new Data.MstCustomer
                                    {
                                        Customer = customer.Article,
                                        Address = customer.Address,
                                        ContactPerson = customer.ContactPerson,
                                        ContactNumber = customer.ContactNumber,
                                        CreditLimit = customer.CreditLimit,
                                        TermId = terms.FirstOrDefault().Id,
                                        TIN = customer.TaxNumber,
                                        WithReward = false,
                                        RewardNumber = null,
                                        RewardConversion = 4,
                                        AccountId = 64,
                                        EntryUserId = defaultSettings.FirstOrDefault().PostUserId,
                                        EntryDateTime = DateTime.Now,
                                        UpdateUserId = defaultSettings.FirstOrDefault().PostUserId,
                                        UpdateDateTime = DateTime.Now,
                                        IsLocked = true,
                                        DefaultPriceDescription = null,
                                        CustomerCode = customer.ManualArticleCode,
                                    };

                                    posData.MstCustomers.InsertOnSubmit(newCustomer);
                                    posData.SubmitChanges();

                                    Console.WriteLine("Save Successful!");
                                    Console.WriteLine();
                                }
                            }
                            else
                            {
                                Console.WriteLine("Cannot Save Customer: " + customer.Article);
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