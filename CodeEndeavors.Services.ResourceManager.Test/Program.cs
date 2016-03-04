using CodeEndeavors.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Client = CodeEndeavors.Services.ResourceManager.Client;
using DomainObjects = CodeEndeavors.Services.ResourceManager.Shared.DomainObjects;

namespace CodeEndeavors.Services.ResourceManager.Test
{
    class Program
    {

        private static string AquireUserId()
        {
            return "5";
        }

        private static Client.RepositoryService RepositoryService
        {
            get { return Client.RepositoryService.Resolve(); }
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            string url = "http://resourcemanager.servicehost.dev";
            Console.Write("Url (ENTER={0}):", url);
            string endpoint = Console.ReadLine();
            if (String.IsNullOrEmpty(endpoint) == false)
                url = endpoint;

            Console.WriteLine("Initializing Service ({0})", url);
            //Log.Configure("log4net.config", "SampleAppLogger");

            Client.RepositoryService.Register(url, 600000);
            RepositoryService.SetAquireUserIdDelegate(AquireUserId);
            RepositoryService.ConfigureLogging("Debug", (string level, string message) =>
                {
                    RecordMessage(string.Format("{0}:{1}", level, message));
                });

            string command = "";
            while (command.ToLower() != "exit")
            {
                try
                {
                    Console.Write(">");
                    command = Console.ReadLine();

                    var commandParts = command.Split(' ');
                    var method = commandParts[0];

                    switch (method.ToLower())
                    {
                        case "getresources":
                            {
                                DoGetResources(commandParts);
                                break;
                            }
                        default:
                            {
                                RecordMessage("Unknown Command");
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }


        private static Client.ClientCommandResult<List<DomainObjects.Resource>> DoGetResources(string[] commandParts)
        {
            var resourceType = "Videre.Core.Models.Localization";
            var includeAudits = true;
            if (commandParts.Length > 1)
                resourceType = commandParts[1];
            if (commandParts.Length > 2)
                includeAudits = bool.Parse(commandParts[2]);

            var cr = RepositoryService.GetResources(resourceType, includeAudits);
            Console.WriteLine(cr.Data.ToJson(true));
            return cr;
        }

        private static void RecordMessage(string Message)
        {
            Console.WriteLine(Message);
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
