﻿using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using OutlookNotificationsAPI.Models;
using OutlookNotificationsAPI.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace OutlookNotificationsAPI.Controllers
{
    public class NotifyController : ApiController
    {
        /// <summary>
        /// Responds to requests generated by subscriptions registered with
        /// the Outlook Notifications REST API. 
        /// </summary>
        /// <param name="validationToken">The validation token sent by Outlook when
        /// validating the Notification URL for the subscription.</param>
        public async Task<HttpResponseMessage> Post(string validationToken = null)
        {
            // If a validation token is present, we need to respond within 5 seconds.
            if (validationToken != null)
            {
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(validationToken);
                return response;
            }

            // Present only if the client specified the ClientState property in the 
            // subscription request. 
            IEnumerable<string> clientStateValues;
            Request.Headers.TryGetValues("ClientState", out clientStateValues);

            if (clientStateValues != null)
            {
                var clientState = clientStateValues.ToList().FirstOrDefault();
                if (clientState != null)
                {
                    // TODO: Use the client state to verify the legitimacy of the notification.
                }
            }

        // Read and parse the request body.
        var content = await Request.Content.ReadAsStringAsync();
        var notifications = JsonConvert.DeserializeObject<ResponseModel<NotificationModel>>(content).Value;

        // TODO: Do something with the notification.
        var entities = new ApplicationDbContext();
        foreach (var notification in notifications)
        {
            // Get the subscription from the database in order to locate the
            // user identifiers. This is used to tap the token cache.
            var subscription = entities.SubscriptionList.FirstOrDefault(s =>
                s.SubscriptionId == notification.SubscriptionId);

            try
            {
                // Get an access token to use when calling the Outlook REST APIs.
                var token = await TokenHelper.GetTokenForApplicationAsync(
                    subscription.SignedInUserID,
                    subscription.TenantID,
                    subscription.UserObjectID,
                    TokenHelper.OutlookResourceID);
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);




                // Send a GET call to the monitored event.
                var responseString = await httpClient.GetStringAsync(notification.Resource);
                var calendarEvent = JsonConvert.DeserializeObject<CalendarEventModel>(responseString);

                // TODO: Do something with the calendar event.
            }
            catch (AdalException)
            {
                // TODO: Handle token error.
            }
            // If the above failed, the user needs to explicitly re-authenticate for 
            // the app to obtain the required token.
            catch (Exception)
            {
                // TODO: Handle exception.
            }
        }


        // Present only if the client specified the SequenceNumber property in the 
        // subscription request. 
        IEnumerable<string> sequenceNumber;
        Request.Headers.TryGetValues("SequenceNumber", out sequenceNumber);

        if (sequenceNumber != null)
        {
            var sequence = sequenceNumber.ToList().FirstOrDefault();
            if (sequence != null)
            {
                using (StreamWriter sw = File.AppendText(fullPath))
                {
                    sw.WriteLine("Sequence number: " + sequence);
                }
            }
        }

        // Present only if the client specified the Resource property in the 
        // subscription request. 
        IEnumerable<string> resourceDetails;
        Request.Headers.TryGetValues("Resource", out resourceDetails);

        if (resourceDetails != null)
        {
            var resourceD = resourceDetails.ToList().FirstOrDefault();
            if (resourceD != null)
            {
                using (StreamWriter sw = File.AppendText(fullPath))
                {
                    sw.WriteLine("Resource: " + resourceD);
                }
            }
        }

        // Present only if the client specified the Resource property in the 
        // subscription request. 
        IEnumerable<string> changeType;
        Request.Headers.TryGetValues("ChangeType", out changeType);

        if (changeType != null)
        {
            var change_Type = changeType.ToList().FirstOrDefault();
            if (change_Type != null)
            {
                using (StreamWriter sw = File.AppendText(fullPath))
                {
                    sw.WriteLine("ChangeType: " + change_Type);
                }
            }
        }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}