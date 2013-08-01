using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using CodeEndeavors.Extensions;
//using StructureMap;

namespace CodeEndeavors.ResourceManager.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //using (var repo = new ResourceRepository("RavenDB", ResourceRepository.RepositoryType.RavenDb))
            //{

            //var connection = "{ type:'AzureBlob', azureBlobStorage:'DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==' }";
            var connection = "{ type:'AzureBlob', azureBlobStorage:'dev' }";

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


    }
}
