using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Raven.Client.Document;

namespace CodeEndeavors.ResourceManager
{
    interface IDataRepository
    {
        T Get<T>(string Key);
        T Save<T>(T Resource, int UserId);
        T Delete<T>(T Resource, int UserId);
        void Purge<T>(T Resource);
        void Dispose();
    }
}
