using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace SatelliteApp
{
    class NetService
    {
        public class gps_data
        {
            public string key { get; set; } = "SATAPPSP";
            public double lat { get; set; }
            public double lon { get; set; }
            public double height { get; set; }
        }

        private static HttpClient _client = new HttpClient();
        async public static Task<HttpResponseMessage> Post(string path, object obj){
            string jsonString = JsonConvert.SerializeObject(obj);
            var response =  await _client.PostAsync(path, new StringContent(jsonString, Encoding.UTF8, "application/json"));
            return response;
        }
        async public static Task<HttpResponseMessage> Get(string path)
        {
            var response = await _client.GetAsync(path);
            return response;
        }
    }
}
