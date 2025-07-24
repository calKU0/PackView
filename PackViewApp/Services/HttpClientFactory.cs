using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PackViewApp.Services
{
    public static class HttpClientFactory
    {
        public static HttpClient CreateDigestClient(string camera, string username, string password)
        {
            var handler = new HttpClientHandler
            {
                PreAuthenticate = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password)
            };

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri($"http://{camera}")
            };

            return client;
        }
    }
}