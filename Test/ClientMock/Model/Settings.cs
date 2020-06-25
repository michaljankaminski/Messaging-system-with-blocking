using System;
using System.Collections.Generic;
using System.Text;

namespace EdcsClient.Model
{
    public class Rabbit
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Port { get; set; }
    }
    public class Postgres
    {
        public string ConnectionString { get; set; }
    }
    public class Settings
    {
        public Rabbit Rabbit { get; set; }
        public User User { get; set; }
        public Postgres Postgres { get; set; }
    }
}
