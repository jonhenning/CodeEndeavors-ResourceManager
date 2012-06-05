using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeEndeavors.ResourceManager.DomainObjects
{
    public class Query<T>
    {
        public Query()
        {

        }
        public Query(Func<T, dynamic> Statement, int Score)
        {
            this.Statement = Statement;
            this.Score = Score;
        }

        public Func<T, dynamic> Statement { get; set; }
        public int Score { get; set; }
    }
}
