using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OutlookPushNotification.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string SubscriptionId { get; set; }
        public string Scope { get; set; }
        public string SubscriptionExpirationDateTime { get; set; }
    }
}