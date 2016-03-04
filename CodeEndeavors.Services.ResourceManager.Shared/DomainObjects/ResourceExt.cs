using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEndeavors.Services.ResourceManager.Shared.DomainObjects
{
    public enum RowStateEnum
    {
        Unchanged = 0,
        Added = 1,
        Modified = 2,
        Deleted = 3
    }

    public partial class Resource
    {
        public RowStateEnum RowState { get; set; }    
    }
}
