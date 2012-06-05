using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CodeEndeavors.ResourceManager
{
    public class RepositoryRequest
    {
        public delegate void RepositoryRequestHandler<T>(RepositoryResult<T> result); //where T : new();
        public static RepositoryResult<T> Execute<T>(RepositoryRequestHandler<T> CodeFunc) //where T : new()
        {
            var result = new RepositoryResult<T>();
            try
            {
                CodeFunc(result);
            }
            catch (Exception ex)
            {
                result.AddError(ex);
            }
            result.StopTime();

            Trace.TraceInformation(result.ToString());

            return result;
        }
    }
}
