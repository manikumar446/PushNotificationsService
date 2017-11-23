using System.Collections.Generic;

namespace OutlookPushNotification.Models
{
    public class ResponseModel<T>
    {
        public List<T> Value { get; set; }
    }
}
