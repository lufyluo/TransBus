using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using HttpTargetHost.Models;

namespace HttpTargetHost.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value:"+ id;
        }

        //// POST api/values
        //public string Post([FromBody]string value)
        //{
        //    return "body:" + value;
        //}
        [HttpPost]
        public string Post([FromBody]PostModel value)
        {
            return "body:" + value.Name;
        }
        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
