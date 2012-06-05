using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeEndeavors.ResourceManager.Shared.DomainObjects
{
    public class Message
    {
        public Message() { }

        public Message(string id, string text, bool isError)
        {
            this.id = id;
            this.text = text;
            this.isError = isError;
        }

        public string id { get; set; }
        public string text { get; set; }
        public bool isError { get; set; }
    }
}
