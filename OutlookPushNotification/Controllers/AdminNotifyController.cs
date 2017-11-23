using Newtonsoft.Json;
using OutlookPushNotification.DAL;
using OutlookPushNotification.Dto;
using OutlookPushNotification.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;

namespace OutlookPushNotification.Controllers
{
    public class AdminNotifyController : ApiController
    {
        string fullPath = "D:\\home\\site\\wwwroot\\AdminCiscoWebExNotifier.txt";

        [HttpGet]
        public async Task<HttpResponseMessage> CheckUser(string email)
        {
            Boolean requestStatus = true;
            try
            {
                string token = "";
                token = await GetToken();
                if (token!=null)
                {
                    var dbContext = new AdminDbHelper();
                    var user = dbContext.GetUser(email);
                    if (user == null)
                    {
                        dbContext.UpdateUser(email);
                        requestStatus = await CreateSubscription(token, email);
                    }
                    else
                    {
                        requestStatus = await RenewSubscription(user.SubscriptionId, token, email);
                    }
                }
                else
                {
                    requestStatus = false;
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter sw = File.AppendText(fullPath))
                {
                    sw.WriteLine(ex.Message);
                    sw.WriteLine(ex.StackTrace);
                }
                requestStatus = false;
            }

            if (requestStatus)
                return Request.CreateResponse(HttpStatusCode.OK);
            else
                return Request.CreateResponse(HttpStatusCode.InternalServerError);

        }

        private async Task<Boolean> CreateSubscription(string token, string email)
        {
            using (var httpClient = new HttpClient())
            {
                SubscriptionRequestData requsetData = new SubscriptionRequestData();
                requsetData.OdataType = "#Microsoft.OutlookServices.PushSubscription";
                requsetData.Resource = "https://outlook.office.com/api/v2.0/me/events/?$filter=SingleValueExtendedProperties%2FAny(ep%3A%20ep%2FPropertyId%20eq%20'String%20{00020329-0000-0000-C000-000000000046}%20Name%20cecp-7e24ee5e-204e-4eeb-aa0f-788af20fc21c'%20and%20ep%2FValue%20ne%20null)";
                requsetData.NotificationURL = "https://exchangepushnotifications.azurewebsites.net/api/AdminNotify/CiscoWebExDemoNotifier";
                requsetData.ClientState = WebConfigurationManager.AppSettings["clientState"];
                requsetData.ChangeType = "Created,Updated,Deleted";


                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var stringPayload = await Task.Run(() => JsonConvert.SerializeObject(requsetData));
                var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync("https://outlook.office.com/api/v2.0/me/subscriptions", httpContent);

                if (httpResponse.IsSuccessStatusCode)
                {
                    SubscriptionResponseData responseData = await httpResponse.Content.ReadAsAsync<SubscriptionResponseData>();
                    var dbContext = new AdminDbHelper();
                    //save subscription details in DB.
                    dbContext.UpdateUser(email, responseData.Id);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private async Task<Boolean> RenewSubscription(string subscriptionId, string token, string email)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("https://outlook.office.com/");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), "api/v2.0/me/subscriptions/" + subscriptionId);

                var httpResponse = await httpClient.SendAsync(request);
                if (httpResponse.IsSuccessStatusCode)
                {
                    SubscriptionResponseData responseData = await httpResponse.Content.ReadAsAsync<SubscriptionResponseData>();
                    var dbContext = new AdminDbHelper();
                    //save subscription details in DB.
                    dbContext.UpdateUser(email, responseData.Id);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private async Task<String> GetToken()
        {
            using (var httpClient = new HttpClient())
            {
                var clientId = "34d6e48b-14c5-49dd-aacf-92420cbd877c";
                var clientSecret = "akmduESON_#uaKON76766?-";
                var grantType = "client_credentials";
                var requestData = new List<KeyValuePair<string, string>>();

                httpClient.BaseAddress = new Uri("https://login.microsoftonline.com/");

                requestData.Add(new KeyValuePair<string, string>("client_id", clientId));
                requestData.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
                requestData.Add(new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"));
                requestData.Add(new KeyValuePair<string, string>("grant_type", grantType));

                var request = new HttpRequestMessage(HttpMethod.Post, "397e3afd-a61f-40a1-99fa-5092d9f68c1c/oauth2/v2.0/token");
                request.Content = new FormUrlEncodedContent(requestData);
                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    TokenResponseData responseData = await response.Content.ReadAsAsync<TokenResponseData>();
                    return responseData.access_token;
                }
                else
                {
                    return null;
                }
            }
        }

        [HttpPost]
        public async Task<HttpResponseMessage> CiscoWebExDemoNotifier(string validationToken = null)
        {
            try
            {
                // If a validation token is present, we need to respond within 5 seconds.
                if (validationToken != null)
                {
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(validationToken);
                    // Create a file to write to.
                    using (StreamWriter sw = File.AppendText(fullPath))
                    {
                        sw.WriteLine(DateTime.Now);
                        sw.WriteLine(validationToken);
                    }
                    return response;
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(fullPath))
                    {
                        sw.WriteLine(DateTime.Now);
                    }

                    //get the notification details and write them to a log file.
                    var content = await Request.Content.ReadAsStringAsync();
                    var notifications = JsonConvert.DeserializeObject<ResponseModel<NotificationModel>>(content).Value;

                    using (StreamWriter sw = File.AppendText(fullPath))
                    {
                        sw.WriteLine(content);
                    }

                    //TODO: Process the notification
                    //foreach (var notification in notifications)
                    //{
                    //    if (!notification.ChangeType.Equals("Updated"))
                    //    {
                    //        var response = Request.CreateResponse(HttpStatusCode.OK);
                    //        response.Content = new StringContent("success");
                    //        return response;
                    //    }

                    //    var dbContext = new DbHelper();
                    //    var user = dbContext.GetUserBySubscriptionId(notification.SubscriptionId);

                    //    var httpClient = new HttpClient();
                    //    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

                    //    // Send a GET call to the monitored event.
                    //    var responseString = await httpClient.GetStringAsync(notification.Resource);
                    //    var calendarEvent = JsonConvert.DeserializeObject<CalendarEventModel>(responseString);


                    //    //update the event body
                    //    //calendar API documentation:https://msdn.microsoft.com/en-us/office/office365/api/calendar-rest-operations#UpdateEvents 
                    //    var httpContent = new StringContent("{\"Body\": { \"ContentType\" : \"HTML\", \"Content\": \"------NEW MEETING BODY-----\"} }", Encoding.UTF8, "application/json");
                    //    var method = new HttpMethod("PATCH");
                    //    httpClient = new HttpClient();
                    //    httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + user.AccessToken);

                    //    //Rest API to update calendar Event
                    //    var request = new HttpRequestMessage(method, "https://outlook.office.com/api/v2.0/me/events/" + calendarEvent.Id)
                    //    {
                    //        Content = httpContent
                    //    };

                    //    var httpResponse = await httpClient.SendAsync(request);

                    //    if (httpResponse.IsSuccessStatusCode)
                    //    {
                    //        Console.WriteLine("Event updated successfully");
                    //    }
                    //    else
                    //    {
                    //        Console.WriteLine("Event updated failed" + httpResponse.StatusCode.ToString());
                    //    }
                    //}

                    var response1 = Request.CreateResponse(HttpStatusCode.OK);
                    response1.Content = new StringContent("success");
                    return response1;
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter sw = File.AppendText(fullPath))
                {
                    sw.WriteLine(DateTime.Now);
                    sw.WriteLine(ex.Message);
                    sw.WriteLine(ex.StackTrace);
                }
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent("success");
                return response;
            }
        }
    }
}
