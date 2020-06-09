﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PowerApps.Samples
{
    /// <summary>
    /// This program demonstrates use of functions and actions with the 
    /// Common Data Service Web API.
    /// </summary>
    class Program
    {
        //Get environment configuration data from the connection string in the App.config file.
        static readonly string connectionString =
            ConfigurationManager.ConnectionStrings["Connect"].ConnectionString;
        static readonly ServiceConfig config = new ServiceConfig(connectionString);

        // Save the URIs for entity records created in this sample. so they
        // can be deleted later.
        static List<Uri> entityUris = new List<Uri>();

        static void Main()
        {
            try
            {
                // Use the wrapper class that handles message processing, error handling, and more.
                using (CDSWebApiService svc = new CDSWebApiService(config))
                {
                    Console.WriteLine("-- Starting function and actions demonstration --\n");

                    CreateRequiredRecords(svc);

                    #region Call an unbound function with no parameters.
                    //Retrieve the current user's full name from the WhoAmI function:
                    JToken currentUser;

                    try
                    {
                        Console.Write("Getting information on the current user..");
                        currentUser = svc.Get("WhoAmI");

                        //Obtain the user's ID, and then the full name
                        Guid myUserId = (Guid)currentUser["UserId"];
                        JToken lookup = svc.Get("systemusers(" + myUserId + ")?$select=fullname");
                        currentUser = lookup["fullname"].ToString();

                        Console.WriteLine("completed.");
                        Console.WriteLine("Current user's full name is '{0}'.", currentUser);
                    }
                    catch (ServiceException e)
                    {
                        if (e.StatusCode == (int)HttpStatusCode.NotFound)
                        {
                            Console.WriteLine("Current user does not have a full name.");
                        }
                        else
                        {
                            Console.WriteLine("failed.");
                            throw e;
                        }
                    }
                    #endregion Call an unbound function with no parameters.

                    #region Delete created records

                    // Delete (or keep) all the created entity records.
                    Console.Write("\nDo you want these entity records deleted? (y/n) [y]: ");
                    String answer = Console.ReadLine().Trim();

                    if (!(answer.StartsWith("y") || answer.StartsWith("Y") || answer == string.Empty))
                        entityUris.Clear();

                    foreach (Uri entityUrl in entityUris) svc.Delete(entityUrl);

                    #endregion Delete created records 
                }
            }
            catch (ServiceException e)
            {
                Console.WriteLine("Message send response: status code {0}, {1}",
                    e.StatusCode, e.ReasonPhrase);
            }
        }

        /// <summary> 
        /// Creates the entity instances used by this sample.
        /// </summary>
        private static void CreateRequiredRecords(CDSWebApiService svc)
        {
            Console.WriteLine("--- Creating Required Records ---");

            // Create a parent account
            Uri account1Uri;
            JObject account1 = new JObject() { { "name", "Fourth Coffee" } };
            try
            {
                Console.Write("Creating parent account..");
                account1Uri = svc.PostCreate("accounts", account1);

                Console.WriteLine("completed.");
                entityUris.Add(account1Uri); // Track any created records
            }
            catch (ServiceException e)
            {
                Console.WriteLine("failed.");
                throw e;
            }

            // Create an incident with three associated tasks
            Uri incident1Uri;

            JArray tasks = new JArray(new JObject[]
            {
                JObject.Parse(@"{subject: 'Task 1', actualdurationminutes: 30}"),
                JObject.Parse(@"{subject: 'Task 2', actualdurationminutes: 30}"),
                JObject.Parse(@"{subject: 'Task 3', actualdurationminutes: 30}")
            });

            JObject incident1 = new JObject()
            {
                { "title", "Sample Case" },
                { "customerid_account@odata.bind", account1Uri },
                { "Incident_Tasks", tasks }
            };

            try
            {
                Console.Write("Creating an incident..");
                incident1Uri = svc.PostCreate("incidents", incident1);

                Console.WriteLine("completed.");
                entityUris.Add(incident1Uri);
            }
            catch (ServiceException e)
            {
                Console.WriteLine("failed.");
                throw e;
            }

            // Close the associated tasks (so that they represent completed work).
            try
            {
                Console.Write("Setting each incident task to completed..");
                JToken incidentTaskRefs = svc.Get(incident1Uri + "/Incident_Tasks/$ref");

                // Property value set used to mark tasks as completed. 
                JObject completeCode = JObject.Parse(@"{statecode: 1, statuscode: 5}");

                foreach (var taskref in incidentTaskRefs)
                {
                    // TODO Set the completion code for each task
                    //svc.Patch(taskref., completeCode);
                }
                Console.WriteLine("done.");
            }
            catch (ServiceException e)
            {
                Console.WriteLine("failed.");
                throw e;
            }

            /**
            //Create another account and associated opportunity (required for CloseOpportunityAsWon).
            JObject account2 = new JObject();
            string account2Uri;
            account2["name"] = "Coho Winery";
            account2["opportunity_customer_accounts"] = JArray.Parse(@"[{ name: 'Opportunity to win' }]");
            request = new HttpRequestMessage(HttpMethod.Post, "accounts");
            request.Content = new StringContent(account2.ToString(), Encoding.UTF8, "application/json");
            response = httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).Result;
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                Console.WriteLine("Another Account is created and associated Opportunity");
                account2Uri = response.Headers.GetValues("OData-EntityId").FirstOrDefault();
                entityUris.Add(account2Uri);
            }
            else
            { throw new Exception(string.Format("Failed to create another account and associated opportunity", response.Content)); }
            


            //Retrieve the URI to the opportunity.
            JObject custOpporRefs;
            response = httpClient.GetAsync(account2Uri + "/opportunity_customer_accounts/$ref",
                HttpCompletionOption.ResponseContentRead).Result;
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Retrieving the URI to the Opportunity");
                custOpporRefs = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                opportunity1Uri = custOpporRefs["value"][0]["@odata.id"].ToString();
            }
            else
            { throw new Exception(string.Format("Failed to retrieve the URI to the opportunity", response.Content)); }



            //Create a contact to use with custom action sample_AddNoteToContact 
            contact1 = JObject.Parse(@"{firstname: 'Jon', lastname: 'Fogg'}");
            request = new HttpRequestMessage(HttpMethod.Post, "contacts");
            request.Content = new StringContent(contact1.ToString(), Encoding.UTF8, "application/json");
            response = httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead).Result;
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                Console.WriteLine("Contact record is created");
                contact1Uri = response.Headers.GetValues("OData-EntityId").FirstOrDefault();
                entityUris.Add(contact1Uri);
            }
            else
            { throw new Exception(string.Format("Failed to create a contact to use with custom action", response.Content)); }
            **/
        }
    }
}
