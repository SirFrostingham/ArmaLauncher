using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;

namespace ArmaLauncher.Helpers
{
    public class ApiManager
    {

        public static T Execute<T>(RestRequest request, string baseUrl, string username, string password) where T : new()
        {
            var response = new RestResponse<T>();

            var client = new RestClient();
            client.BaseUrl = baseUrl;

            // set a http request timeout to something reasonable
            request.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["HttpRequestTimeoutInMilliseconds"]);

            //TODO:areed if we expand to APIs with username/password, we need to get this working
            if (username != null || password != null)
            {
                client.Authenticator = new HttpBasicAuthenticator(username, password);
                request.AddParameter("AccountSid", username, ParameterType.UrlSegment);
            }

            try
            {
                response = (RestResponse<T>)client.Execute<T>(request);
            }
            catch (Exception)
            {
            }

            return response.Data;
        }
    }
}
