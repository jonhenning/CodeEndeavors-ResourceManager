using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Newtonsoft.Json;
using CodeEndeavors.Extensions;
using System.Reflection;
//using StructureMap;

namespace CodeEndeavors.ResourceManager.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            var connection = "{ type:'File', resourceDir:'~/App_Data/FileDb', cacheConnection: {cacheName: 'MyCache', cacheType: 'CodeEndeavors.Distributed.Cache.Client.InMemory.InMemoryCache'} }";

            using (var repo = new ResourceRepository(connection))
            {
                var type = typeof(MyObj).ToString();

                var resources = repo.GetResources<MyObj>(type);
                Console.WriteLine(resources.ToJson());

                var o = new MyObj() { Text = "foo" };
                var userId = "1";
                repo.DeleteAll<string>(type);

                var resource = new DomainObjects.Resource<MyObj>()
                {
                    Type = type,
                    Sequence = 1,
                    Data = o
                };

                resource = repo.StoreResource(resource, userId);
                repo.SaveChanges();

                var r = repo.GetResourceById<MyObj>(resource.Id);
                Console.WriteLine(r.ToJson());

                resources = repo.GetResources<MyObj>(type);
                Console.WriteLine(resources.ToJson());

                repo.Delete<MyObj>(resource);
                repo.SaveChanges();

                resources = repo.GetResources<MyObj>(type);
                Console.WriteLine(resources.ToJson());
            
            }


            Console.WriteLine("DONE!");
            Console.ReadLine();

        }

        public class MyObj
        {
            public string Id { get; set; }
            public string Text { get; set; }
        }


        public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            if (assemblyName.Name != args.Name)
                return Assembly.LoadWithPartialName(assemblyName.Name);
            return null;
        }
    }
}
