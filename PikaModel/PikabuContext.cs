using System;
using Microsoft.EntityFrameworkCore;

namespace PikaModel.Models
{
    public partial class PikabuContext : DbContext
    {
        private string GetDevConnectionString()
        {
            // "Server=84.201.143.249;Port=3306;Database=pikabu;Uid=lam0x86;Pwd=lam0xPIKABU!"
            return Environment.GetEnvironmentVariable("DEV_CONNECTION_STRING");
        }
    }
}