using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIGateway.Models
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string AccessSecretKey { get; set; }
        public string InvitationSecretKey { get; set; }
        public string ValidAudience { get; set; }
        public string SessionTokenIssuer { get; set; }
        public string ValidIssuer { get; set; }
        public string DashboardAudience { get; set; }
        public string RoutesAppAudience { get; set; }
        public string ScreenAudience { get; set; }
        public string BusValidatorAudience { get; set; }
        public string InvitationTokenAudience { get; set; }
    }
}
