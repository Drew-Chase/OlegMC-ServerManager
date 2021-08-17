﻿using Microsoft.AspNetCore.Mvc;
using OlegMC.REST_API.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OlegMC.REST_API.Controllers
{
    public class SetupController : Controller
    {
        [Route("/")]
        public IActionResult Login()
        {
            return View();
        }

        [Route("/login/{username}/{password}/{port}/{protocol}")]
        public IActionResult Login(string username, string password, int port, string protocol)
        {
            Global.LogIn(username, password, port, protocol);
            return Ok();
        }
        [Route("/logout")]
        public IActionResult LogOut()
        {
            Global.LogOut();
            return Ok();
        }
    }
}