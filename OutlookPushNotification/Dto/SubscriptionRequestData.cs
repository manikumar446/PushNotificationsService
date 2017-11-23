using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OutlookPushNotification.Dto
{
    public class SubscriptionRequestData
    {
            [JsonProperty("@odata.type")]
            public string OdataType { get; set; }

            [JsonProperty("Resource")]
            public string Resource { get; set; }

            [JsonProperty("NotificationURL")]
            public string NotificationURL { get; set; }

            [JsonProperty("ChangeType")]
            public string ChangeType { get; set; }

            [JsonProperty("ClientState")]
            public string ClientState { get; set; }
    }
}