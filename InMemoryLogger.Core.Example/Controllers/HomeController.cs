using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using InMemoryLogger.Core.Example.Models;

namespace InMemoryLogger.Core.Example.Controllers
{
        public class HomeController : Controller
        {
            public HomeController()
            {

            }


            public async Task<IActionResult> Index()
            {
                return View();
            }




            public IActionResult Error()
            {
                throw new Exception("This is Test Exception From Home Controller");
            }
        }
    
}
