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
        // Data Context
        // ============
        private static Data.POSDatabaseDataContext posData;

        // ============
        // GET Customer
        // ============
        public void GetCustomer(String database, String apiUrlHost)
        {
            try
            {
                DateTime dateTimeToday = DateTime.Now;
                String currentDate = dateTimeToday.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + apiUrlHost + "/api/get/POSIntegration/customer/" + currentDate);
                httpWebRequest.Method = "GET";
                httpWebRequest.Accept = "application/json";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    List<POSIntegrator.MstCustomer> customerLists = (List<POSIntegrator.MstCustomer>)js.Deserialize(result, typeof(List<POSIntegrator.MstCustomer>));

                    foreach (var customerList in customerLists)
                    {
                        var customerData = new POSIntegrator.MstCustomer()
                        {
                            ManualArticleCode = customerList.ManualArticleCode,
                            Article = customerList.Article,
                            Address = customerList.Address,
                            ContactPerson = customerList.ContactPerson,
                            ContactNumber = customerList.ContactNumber,
                            Term = customerList.Term,
                            TaxNumber = customerList.TaxNumber,
                            CreditLimit = customerList.CreditLimit
                        };

                        String jsonPath = "d:/innosoft/json/master";
                        String fileName = "customer-" + customerList.ManualArticleCode;

                        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                        {
                            fileName = fileName.Replace(c, '_');
                        }

                        String json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(customerData);
                        String jsonFileName = jsonPath + "\\" + fileName + ".json";

                        var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                        posData = new Data.POSDatabaseDataContext(newConnectionString);

                        var customers = from d in posData.MstCustomers
                                        where d.CustomerCode.Equals(customerList.ManualArticleCode)
                                        && d.CustomerCode != null
                                        select d;

                        if (customers.Any())
                        {
                            Boolean foundChanges = false;

                            if (!foundChanges)
                            {
                                if (!customers.FirstOrDefault().Customer.Equals(customerList.Article))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!customers.FirstOrDefault().Address.Equals(customerList.Address))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!customers.FirstOrDefault().ContactPerson.Equals(customerList.ContactPerson))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!customers.FirstOrDefault().ContactNumber.Equals(customerList.ContactNumber))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!customers.FirstOrDefault().MstTerm.Term.Equals(customerList.Term))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (!customers.FirstOrDefault().TIN.Equals(customerList.TaxNumber))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (!foundChanges)
                            {
                                if (Convert.ToDecimal(customers.FirstOrDefault().CreditLimit) != Convert.ToDecimal(customerList.CreditLimit))
                                {
                                    foundChanges = true;
                                }
                            }

                            if (foundChanges)
                            {
                                File.WriteAllText(jsonFileName, json);
                                Console.WriteLine("Updating Customer...");

                                UpdateCustomer(database);
                            }
                        }
                        else
                        {
                            var terms = from d in posData.MstTerms
                                        where d.Term.Equals(customerList.Term)
                                        select d;

                            if (terms.Any())
                            {
                                File.WriteAllText(jsonFileName, json);
                                Console.WriteLine("Saving Customer...");

                                UpdateCustomer(database);
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
        // UPDATE Customer
        // ===============
        public void UpdateCustomer(String database)
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
                    POSIntegrator.MstCustomer customer = json_serializer.Deserialize<POSIntegrator.MstCustomer>(json);

                    var newConnectionString = "Data Source=localhost;Initial Catalog=" + database + ";Integrated Security=True";
                    posData = new Data.POSDatabaseDataContext(newConnectionString);

                    var accounts = from d in posData.MstAccounts
                                   select d;

                    if (accounts.Any())
                    {
                        String customerTerm = customer.Term;

                        var terms = from d in posData.MstTerms
                                    where d.Term.Equals(customerTerm)
                                    select d;

                        if (terms.Any())
                        {
                            var customers = from d in posData.MstCustomers
                                            where d.CustomerCode.Equals(customer.ManualArticleCode)
                                            && d.CustomerCode != null
                                            select d;

                            if (!customers.Any())
                            {
                                var defaultSettings = from d in posData.SysSettings select d;

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

                                Console.WriteLine("Customer: " + customer.Article);
                                Console.WriteLine("Save Successful!");
                                Console.WriteLine();

                                File.Delete(file);
                            }
                            else
                            {
                                var defaultSettings = from d in posData.SysSettings select d;

                                var updateCustomer = customers.FirstOrDefault();
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

                                Console.WriteLine("Customer: " + customer.Article);
                                Console.WriteLine("Update Successful!");
                                Console.WriteLine();

                                File.Delete(file);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Customer: " + customer.Article);
                            Console.WriteLine("Save Failed! Term Mismatch!");
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
