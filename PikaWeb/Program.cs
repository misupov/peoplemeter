﻿using System;
using System.IO;
using System.Net;
using LetsEncrypt;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace PikaWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var now = DateTimeOffset.Now;
            Console.WriteLine("Application started at: " + now);
            return WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, 443, listenOptions =>
                    {
                        listenOptions.UseLetsEncrypt();
                    });
                    options.Listen(IPAddress.Any, 80);
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();
        }
    }
}
