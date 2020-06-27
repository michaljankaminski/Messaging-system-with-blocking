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
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public int ThreadId { get; set; }
    }
}