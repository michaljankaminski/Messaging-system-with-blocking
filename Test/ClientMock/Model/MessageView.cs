using System;
using System.Collections.Generic;
using System.Text;

namespace EdcsClient.Model
{
    public class MessageView
    {
        public User Sender { get; set; }
        public User Receiver { get; set; }
        public string Content { get; set; }
    }
}