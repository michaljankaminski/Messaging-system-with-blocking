using System;
using System.Collections.Generic;
using System.Text;

namespace EdcsServer.Model
{
    public class Message
    {
        public int Sender { get; set; }
        public int Receiver { get; set; }
        public string Content { get; set; }
    }
}
