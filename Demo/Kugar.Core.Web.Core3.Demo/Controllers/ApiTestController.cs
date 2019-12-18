using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Kugar.Core.Web.Core3.Demo.Controllers
{
    public class ApiTestController : ControllerBase
    {
        [FromBodyJson()]
        public IActionResult test1(List<(string productid,int qty)> details)
        {

            return Content("success");
        }
    }
}