using System;
using System.Collections.Generic;
using System.Text;

namespace EdcsClient.Model
{
    public class Message
    {
        public int Sender { get; set; }
        public int Receiver { get; set; }
        public string Content { get; set; }
        public DateTime Received { get; set; }
        public int ThreadId { get; set; }
    }
}