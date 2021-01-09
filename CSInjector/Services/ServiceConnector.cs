using System;
using System.Net;

namespace CshapInstrumenter.Services
{
    class ServiceConnector: WebClient
    {
        WebClient webClinet;

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 20 * 60 * 1000;
            return w;
        }

        public string FormatService(String stringToFormat)
        {
            String service_url = "https://ats.cerner.com:8443/formatter/getModifiedSignature";
        
            //WebRequest wrGETURL;
            //wrGETURL = WebRequest.Create(service_url);
            webClinet = new WebClient();

            //TODO Temprary Fix - Ned to convert this to encoding for all scenarios.
            //stringToFormat = stringToFormat.Replace("[","%5B");
            //stringToFormat = stringToFormat.Replace("]", "%5D");
            stringToFormat = WebUtility.UrlEncode(stringToFormat);
            webClinet.QueryString.Add("signature", stringToFormat);
            string signature = webClinet.DownloadString(service_url);
            return signature;
        }

        public String CacheService(String soulutionName, String domain, String eodCreationDate)
        {
          
            String service_url = "https://ats.cerner.com:8443/solutioninfo/getSolutionData?";
            
            if (soulutionName != null)
            {
                service_url = service_url + "&" + "solutionName" + "=" + soulutionName;
            }
            if (domain != null)
            {
                //webClinet.QueryString.Add("domain", domain);
                service_url = service_url + "&" + "domain" + "=" + domain;
            }
            if (eodCreationDate != null)
            {
                //webClinet.QueryString.Add("eodCreationDate", eodCreationDate);
                service_url = service_url + "&" + "eodCreationDate" + "=" + eodCreationDate;
            }
            WebRequest request = GetWebRequest(new Uri(service_url));
            string signature = request.GetResponse().ToString();
            return signature;
        }
    }

}
