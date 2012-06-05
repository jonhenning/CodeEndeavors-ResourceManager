using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeEndeavors.ResourceManager.DomainObjects
{
    public enum ScopeMatchType
    {
        Exact,
        Contains,
        Regex,
        StartsWith,
        Not
    }

    //public enum ScopeMatchOperator
    //{
    //    And,
    //    Or
    //}

    public class ScopeMatch
    {
        public ScopeMatch()
        {
            //Items = new List<string>();
        }

        public ScopeMatch(string ExactMatch)
            :this(ScopeMatchType.Exact, ExactMatch, 1, true, false)
        {

        }

        public ScopeMatch(ScopeMatchType Type, string Value, int Score, bool Required)
            : this(Type, Value, Score, Required, false)
        {
        }

        public ScopeMatch(ScopeMatchType Type, string Value, int Score, bool Required, bool Not)
            : this()
        {
            this.Type = Type;
            this.Not = Not;
            this.Score = Score;
            this.Required = Required;
            this.Value = Value;
        }

        public ScopeMatchType Type { get; set; }
        public bool Not { get; set; }
        //public ScopeMatchOperator Operator { get; set; }
        //public List<string> Items { get; set; }
        public string Value { get; set; }
        public int Score { get; set; }
        public bool Required { get; set; }

        //[Newtonsoft.Json.JsonIgnore()]
        //public string Value
        //{
        //    get
        //    {
        //        //todo: allow items count == 0?
        //        if (Items.Count == 1)
        //            return Items[0];
        //        else
        //            throw new Exception(string.Format("Scope contains {0} items.  Accessing it as if it were a single value is not allowed.", Items.Count));
        //    }
        //    set
        //    {
        //        if (Items.Count == 0)
        //            Items.Add(value);
        //        else
        //            Items[0] = value;
        //    }
        //}

        public int MatchScore(string Item)
        {
            return MatchScore(new List<string>() { Item });
        }

        public int MatchScore(List<string> Items)
        {
            var matchScore = 0;
            var matches = 0;
            Value = Value + ""; //change null to empty string
            foreach (var item in Items)
            {
                var nonNullItem = item + "";

                if (Type == ScopeMatchType.Exact)
                    matches += Value.Equals(nonNullItem, StringComparison.OrdinalIgnoreCase) ? (Not ? 0 : 1) : (Not ? 1 : 0);
                else if (Type == ScopeMatchType.Contains)
                    matches += Value.IndexOf(nonNullItem, StringComparison.OrdinalIgnoreCase) > -1 ? (Not ? 0 : 1) : (Not ? 1 : 0);
                else if (Type == ScopeMatchType.StartsWith)
                    matches += Value.IndexOf(nonNullItem, StringComparison.OrdinalIgnoreCase) == 0 ? (Not ? 0 : 1) : (Not ? 1 : 0);
                else if (Type == ScopeMatchType.Regex)
                    matches += Regex.Match(Value, nonNullItem, RegexOptions.IgnoreCase).Success ? (Not ? 0 : 1) : (Not ? 1 : 0);
            }

            //if ((Operator == ScopeMatchOperator.And && matches == Items.Count) || (Operator == ScopeMatchOperator.Or && matches > 0))
            if (matches > 0)
                matchScore = Score;

            return matchScore;
        }

    }
}
