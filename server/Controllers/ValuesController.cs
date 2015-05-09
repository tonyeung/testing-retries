using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace server.Controllers
{
    //[Authorize]
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public HttpResponseMessage Get(int id)
        {
            var nowSeconds = DateTime.UtcNow.Second;
            if (nowSeconds % 2 == 0)
            {
                return Request.CreateResponse(HttpStatusCode.OK, "value");
            }
            else
            {
                System.Threading.Thread.Sleep(1 * 60 * 1000);
                return Request.CreateErrorResponse(HttpStatusCode.ServiceUnavailable, "timeout");
            }
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
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
