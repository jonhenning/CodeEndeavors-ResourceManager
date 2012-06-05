using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeEndeavors.ResourceManager.Shared.DomainObjects;

namespace CodeEndeavors.ResourceManager
{
    public class RepositoryResult<T> 
    {
        public RepositoryResult()
        {
            Data = default(T);
            Context = new Dictionary<string, object>();
            Messages = new List<Message>();
            _startTime = DateTime.UtcNow;
        }

        private DateTime? _startTime = null;
        private DateTime? _stopTime = null;
        private List<Message> Messages { get; set; }
        //private string _errorMessage;
        public T Data { get; set; }
        public Dictionary<string, object> Context { get; set; }
        public bool HasError { get; set; }
        
        //public bool Compressed { get; set; }

        public void AddError(string Text)
        {
            HasError = true;
            Messages.Add(new Message(Text, Text, true));
        }

        public void AddError(Exception Ex)
        {
            HasError = true;
            var s = Ex.Message;
            if (Ex.InnerException != null)
                s += "\r\n" + Ex.InnerException.Message;
            Messages.Add(new Message(s, s, true));
        }

        public void AddErrors(IEnumerable<string> Errors)
        {
            HasError = true;
            foreach (var sErr in Errors)
                Messages.Add(new Message(sErr, sErr, true));
        }

        public void AddMessage(string Text)
        {
            AddMessage(Text, Text);
        }
        public void AddMessage(string Id, string Text)
        {
            Messages.Add(new Message(Id, Text, false));
        }

        public void StopTime()
        {
            _stopTime = DateTime.UtcNow;
        }

        public string ToString()
        {
            return string.Format("Executed request for {0} in {1}ms", typeof(T).ToString(), _startTime.Value.Subtract(_stopTime.Value).TotalMilliseconds);
        }


    }
}
