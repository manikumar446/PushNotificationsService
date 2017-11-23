using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Http;
using OutlookPushNotification.Dto;
using System.Web.Configuration;
using System.Text;
using OutlookPushNotification.DAL;
using OutlookPushNotification.Models;
using System.Net.Http.Headers;

namespace OutlookPushNotification.Controllers
{
    public class NotifyController : ApiController
    {
        /// <summary>
        /// It checks user is already exists with this email or not.
        /// if user already exists, it refreshes the token and renews the push notification subscription.
        /// if user doesn't exist, it sends 404 as response code.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<HttpResponseMessage> CheckUser(string email)
        {
            var dbContext = new DbHelper();
            var user = dbContext.GetUser(email);
            if (user != null)
            {
                var status = await refreshToken(user);
                if (status)
                {
                    await RenewSubscription(user);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                else
                {
                    var response = Request.CreateResponse(HttpStatusCode.NotFound);
                    response.Content = new StringContent("Email doesn't exists");
                    return response;
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        /// <summary>
        /// It makes call to outlook token api by passing the authorization code.
        /// Once it gets the token, creates new subscription or renew the existing subscription.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<HttpResponseMessage> GetAccessToken(string code, string email)
        {
            using (var httpClient = new HttpClient())
            {

                var clientId = WebConfigurationManager.AppSettings["clientId"];
                var clientSecret = WebConfigurationManager.AppSettings["clientSecret"];
                var redirectUri = WebConfigurationManager.AppSettings["redirectUri"];
                var grantType = "authorization_code";
                var requestData = new List<KeyValuePair<string, string>>();

                httpClient.BaseAddress = new Uri("https://login.microsoftonline.com/");

                requestData.Add(new KeyValuePair<string, string>("client_id", clientId));
                requestData.Add(new KeyValuePair<string, string>("code", code));
                requestData.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
                requestData.Add(new KeyValuePair<string, string>("redirect_uri", redirectUri));
                requestData.Add(new KeyValuePair<string, string>("grant_type", grantType));

                var request = new HttpRequestMessage(HttpMethod.Post, "common/oauth2/v2.0/token");
                request.Content = new FormUrlEncodedContent(requestData);

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    TokenResponseData responseData = await response.Content.ReadAsAsync<TokenResponseData>();
                    var dbContext = new DbHelper();
                    User user = null;
                    var status = true;

                    //save token details in DB.
                    user = dbContext.UpdateUser(email, responseData.access_token, responseData.refresh_token, responseData.scope);


                    //check user has already subscribed for notification.
                    //if yes, renew his subscription. Otherwise create new subscription.
                    if (user.SubscriptionId == null || user.SubscriptionId.Trim().Length < 1)
                    {
                        status = await CreateSubscription(responseData.access_token, email);
                    }
                    else
                    {
                        status = await RenewSubscription(user);
                    }

                    if (status)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound);
                    }
                }
                else
                {
                    return Request.CreateResponse(response.StatusCode);
                }
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> SubscribeToNotification(string accessToken, string email = null)
        {
            var status = await CreateSubscription(accessToken, email);

            if (status)
            {
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        private async Task<Boolean> CreateSubscription(string accessToken, string email = null)
        {
            using (var httpClient = new HttpClient())
            {
                SubscriptionRequestData requsetData = new SubscriptionRequestData();
                requsetData.OdataType = "#Microsoft.OutlookServices.PushSubscription";
                requsetData.Resource = "https://outlook.office.com/api/v2.0/me/events/?$filter=SingleValueExtendedProperties%2FAny(ep%3A%20ep%2FPropertyId%20eq%20'String%20{00020329-0000-0000-C000-000000000046}%20Name%20cecp-7e24ee5e-204e-4eeb-aa0f-788af20fc21c'%20and%20ep%2FValue%20ne%20null)";
                requsetData.NotificationURL = "https://exchangepushnotifications.azurewebsites.net/api/Notify/CiscoWebExDemoNotifier";
                requsetData.ClientState = WebConfigurationManager.AppSettings["clientState"];
                requsetData.ChangeType = "Created,Updated,Deleted";


                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                var stringPayload = await Task.Run(() => JsonConvert.SerializeObject(requsetData));
                var httpContent = new StringContent(stringPayload, Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync("https://outlook.office.com/api/v2.0/me/subscriptions", httpContent);

                if (httpResponse.IsSuccessStatusCode)
                {
                    SubscriptionResponseData responseData = await httpResponse.Content.ReadAsAsync<SubscriptionResponseData>();
                    var dbContext = new DbHelper();
                    //save subscription details in DB.
                    dbContext.UpdateSubscriptionDetails(email, responseData.Id, responseData.SubscriptionExpirationDateTime);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private async Task<Boolean> refreshToken(User user)
        {
            using (var httpClient = new HttpClient())
            {
                var clientId = WebConfigurationManager.AppSettings["clientId"];
                var clientSecret = WebConfigurationManager.AppSettings["clientSecret"];
                var redirectUri = WebConfigurationManager.AppSettings["redirectUri"];
                var grantType = "refresh_token";
                var requestData = new List<KeyValuePair<string, string>>();

                httpClient.BaseAddress = new Uri("https://login.microsoftonline.com/");
                requestData.Add(new KeyValuePair<string, string>("client_id", clientId));
                requestData.Add(new KeyValuePair<string, string>("refresh_token", user.RefreshToken));
                requestData.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
                requestData.Add(new KeyValuePair<string, string>("redirect_uri", redirectUri));
                requestData.Add(new KeyValuePair<string, string>("grant_type", grantType));
                requestData.Add(new KeyValuePair<string, string>("scope", user.Scope));

                var request = new HttpRequestMessage(HttpMethod.Post, "common/oauth2/v2.0/token");
                request.Content = new FormUrlEncodedContent(requestData);

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    TokenResponseData responseData = await response.Content.ReadAsAsync<TokenResponseData>();
                    var dbContext = new DbHelper();
                    dbContext.UpdateTokens(responseData.refresh_token, responseData.access_token, user.Id);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private async Task<Boolean> RenewSubscription(User user)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("https://outlook.office.com/");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + user.AccessToken);
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), "api/v2.0/me/subscriptions/" + user.SubscriptionId);

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    //TokenResponseData responseData = await res.Content.ReadAsAsync<TokenResponseData>();
                    //var dbContext = new DbHelper();
                    //dbContext.UpdateTokens(responseData.access_token, responseData.refresh_token, user.Id);
                    //return true;
                }
                else
                {
                    //return false;
                }
            }

            return true;
        }

        ///// <summary>
        ///// This end point receives the push notification and process it.
        ///// For demo purpose we are just writing notification details to log file and sending an email to user mail box.
        ///// </summary>
        ///// <param name="validationToken"></param>
        ///// <returns></returns>
        //[HttpPost]
        //public async Task<HttpResponseMessage> CiscoWebExDemoNotifier(string validationToken = null)
        //{
        //    string fullPath = "D:\\home\\site\\wwwroot\\CiscoWebExNotifier.txt";
           
        //    try
        //    {
        //        // If a validation token is present, we need to respond within 5 seconds.
        //        if (validationToken != null)
        //        {
        //            var response = Request.CreateResponse(HttpStatusCode.OK);
        //            response.Content = new StringContent(validationToken);
        //            // Create a file to write to.
        //            using (StreamWriter sw = File.AppendText(fullPath))
        //            {
        //                sw.WriteLine(DateTime.Now);
        //                sw.WriteLine(validationToken);
        //            }
        //            return response;
        //        }
        //        else
        //        {
        //            using (StreamWriter sw = File.AppendText(fullPath))
        //            {
        //                sw.WriteLine(DateTime.Now);
        //            }

        //            //get the notification details and write them to a log file.
        //            var content = await Request.Content.ReadAsStringAsync();
        //            var notifications = JsonConvert.DeserializeObject<ResponseModel<NotificationModel>>(content).Value;

        //            using (StreamWriter sw = File.AppendText(fullPath))
        //            {
        //                sw.WriteLine(content);
        //            }

        //            //TODO: Process the notification
        //            foreach (var notification in notifications)
        //            {
        //                if (!notification.ChangeType.Equals("Updated"))
        //                {
        //                    var response = Request.CreateResponse(HttpStatusCode.OK);
        //                    response.Content = new StringContent("success");
        //                    return response;
        //                }

        //                var dbContext = new DbHelper();
        //                var user = dbContext.GetUserBySubscriptionId(notification.SubscriptionId);

        //                var httpClient = new HttpClient();
        //                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

        //                // Send a GET call to the monitored event.
        //                var responseString = await httpClient.GetStringAsync(notification.Resource);
        //                var calendarEvent = JsonConvert.DeserializeObject<CalendarEventModel>(responseString);


        //                //update the event body
        //                //calendar API documentation:https://msdn.microsoft.com/en-us/office/office365/api/calendar-rest-operations#UpdateEvents 
        //                var httpContent = new StringContent("{\"Body\": { \"ContentType\" : \"HTML\", \"Content\": \"------NEW MEETING BODY-----\"} }", Encoding.UTF8, "application/json");
        //                var method = new HttpMethod("PATCH");
        //                httpClient = new HttpClient();
        //                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + user.AccessToken);

        //                //Rest API to update calendar Event
        //                var request = new HttpRequestMessage(method, "https://outlook.office.com/api/v2.0/me/events/" + calendarEvent.Id)
        //                {
        //                    Content = httpContent
        //                };

        //                var httpResponse = await httpClient.SendAsync(request);

        //                if (httpResponse.IsSuccessStatusCode)
        //                {
        //                     Console.WriteLine("Event updated successfully");
        //                }
        //                else
        //                {
        //                    Console.WriteLine("Event updated failed" + httpResponse.StatusCode.ToString());
        //                }
        //            }

        //            var response1 = Request.CreateResponse(HttpStatusCode.OK);
        //            response1.Content = new StringContent("success");
        //            return response1;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        using (StreamWriter sw = File.AppendText(fullPath))
        //        {
        //            sw.WriteLine(DateTime.Now);
        //            sw.WriteLine(ex.Message);
        //            sw.WriteLine(ex.StackTrace);
        //        }
        //        var response = Request.CreateResponse(HttpStatusCode.OK);
        //        response.Content = new StringContent("success");
        //        return response;
        //    }
        //}

        /// <summary>
        /// This end point receives the push notification and process it.
        /// For demo purpose we are just writing notification details to log file and sending an email to user mail box.
        /// </summary>
        /// <param name="validationToken"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<HttpResponseMessage> CiscoWebExDemoNotifier(string validationToken = null)
        {
            string fullPath = "D:\\home\\site\\wwwroot\\CiscoWebExNotifier.txt";
            try
            {

                //string fullPath = "C:\\Mani kumar\\workspace\\Cisco\\OutlookPushNotification\\OutlookPushNotification\\CiscoWebExNotifier.txt";

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

                    var emailAddress = "";

                    //TODO: Process the notification
                    foreach (var notification in notifications)
                    {
                        var dbContext = new DbHelper();
                        var user = dbContext.GetUserBySubscriptionId(notification.SubscriptionId);
                        emailAddress = user.Email;

                        //var httpClient = new HttpClient();
                        //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);

                        // Send a GET call to the monitored event.
                        //var responseString = await httpClient.GetStringAsync(notification.Resource);
                        //var calendarEvent = JsonConvert.DeserializeObject<CalendarEventModel>(responseString);

                        // TODO: Do something with the calendar event.
                    }


                    if (emailAddress != null && emailAddress.Trim().Length > 0)
                    {
                        //To send email, please update below two variables with proper values.
                        var emailId = "forTrelloAddin@gmail.com";
                        var password = "Trello#123";

                        using (MailMessage mm = new MailMessage(emailId, emailAddress))
                        {
                            mm.Subject = "Demo Push Notification Message";
                            mm.Body = "Hello,\n Your calendar is created/updated/deleted. \n" + content;
                            mm.IsBodyHtml = false;
                            SmtpClient smtp = new SmtpClient();
                            smtp.Host = "smtp.gmail.com";
                            smtp.EnableSsl = true;
                            NetworkCredential NetworkCred = new NetworkCredential(emailId, password);
                            smtp.UseDefaultCredentials = true;
                            smtp.Credentials = NetworkCred;
                            smtp.Port = 587;
                            smtp.Send(mm);
                        }
                    }

                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent("success");
                    return response;
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
