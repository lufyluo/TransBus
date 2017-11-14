using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HttpClientTestTransfomer
{
    class Program
    {
        private const string Uri = "http://localhost:12912";
        private const string PostUri = "http://localhost:12912/api/values/54";
        static void Main(string[] args)
        {

            //while (true)
            //{
            //    string code = Console.ReadLine();
            //    var method = typeof(Program).GetMethod(code);
            //    method.Invoke(null, null);
            //}
            var re = StringRe();
            Console.WriteLine(re);
            Console.ReadLine();
        }
        public static string StringRe() {
            var result = Regex.Match(PostUri, @"^http[s]{0,1}://.+:(\d+)", RegexOptions.IgnoreCase);
            if (result.Groups.Count > 1)
            {
                return result.Groups[1].Value;
            }
            return "null";
        }

        public static void test()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("TransToURL", "http://localhost:15496/api/values/5");
            // 创建一个异步GET请求，当请求返回时继续处理
            httpClient.GetAsync(Uri).ContinueWith(
                (requestTask) =>
                {
                    HttpResponseMessage response = requestTask.Result;
                    // 确认响应成功，否则抛出异常
                    response.EnsureSuccessStatusCode();
                    // 异步读取响应为字符串
                    response.Content.ReadAsStringAsync().ContinueWith(
                        (readTask) => Console.WriteLine(readTask.Result));
                });
        }
        public static async Task Post()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("TransToURL", "http://localhost:15496/api/values");
                var values = new List<KeyValuePair<string, string>>();
                values.Add(new KeyValuePair<string, string>("Id", "1"));
                values.Add(new KeyValuePair<string, string>("Name", "world"));

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync(PostUri, content);

                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseString);
            }
        }
    }
}
