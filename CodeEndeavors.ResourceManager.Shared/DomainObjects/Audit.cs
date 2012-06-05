using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeEndeavors.ResourceManager.DomainObjects
{
    public class Audit
    {
        public Audit()
        {

        }
        
        public Audit(string UserId, DateTime Date, string Action)
        {
            this.UserId = UserId;
            this.Date = Date;
            this.Action = Action;
        }

        public string UserId { get; set; }
        public DateTime Date { get; set; }
        public string Action { get; set; }
    }
}
