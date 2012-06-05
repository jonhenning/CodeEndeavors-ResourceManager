using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using StructureMap;

namespace CodeEndeavors.ResourceManager.Test
{
    class Program
    {
        static void Main(string[] args)
        {

            using (var repo = new ResourceRepository("RavenDB", ResourceRepository.RepositoryType.RavenDb))
            {
                //foreach (var o in repo.Get<string>())
                //{
                //    repo.Purge(o);
                //}

                var resource = repo.Get<string>("foo3");
                if (resource == null)
                {
                    resource = new DomainObjects.Resource<string>()
                    {
                        Key = "foo3",
                        Sequence = 1
                    };
                }
                else
                {
                    Console.WriteLine("Already Exists! " + resource.Audit.Count);
                }
                resource = JsonConvert.DeserializeObject<DomainObjects.Resource<string>>(JsonConvert.SerializeObject(resource));
                //resource.Key = "foo3";
                repo.Save(resource, 3);

                Console.WriteLine(JsonConvert.SerializeObject(repo.Get<string>()));
            }
            Console.WriteLine("DONE!");
            Console.ReadLine();

        }
    }
}
