using CodeEndeavors.Extensions;
using CodeEndeavors.ServiceHost.Common.Client;
using CodeEndeavors.ServiceHost.Common.Services;
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
            get { return ServiceLocator.Resolve<Client.RepositoryService>(); }
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

            ServiceLocator.Register<Client.RepositoryService>(url, 600000);
            RepositoryService.SetAquireUserIdDelegate(AquireUserId);
            RepositoryService.ConfigureLogging("Debug", (string level, string message) =>
                {
                    recordMessage(string.Format("{0}:{1}", level, message));
                });

            initializeCache();

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
                        case "saveresources":
                            {
                                DoSaveResources(commandParts);
                                break;
                            }
                        case "obtainlock":
                            {
                                DoObtainLock(commandParts);
                                break;
                            }
                        case "removelock":
                            {
                                DoRemoveLock(commandParts);
                                break;
                            }
                        default:
                            {
                                recordMessage("Unknown Command");
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


        private static void DoGetResources(string[] commandParts)
        {
            var resourceType = "Videre.Core.Models.Localization";
            var includeAudits = true;
            if (commandParts.Length > 1)
                resourceType = commandParts[1];
            if (commandParts.Length > 2)
                includeAudits = bool.Parse(commandParts[2]);

            var cr = RepositoryService.GetResources(resourceType, includeAudits);
            Console.WriteLine(cr.Data.ToJson(true));
        }

        private static void DoSaveResources(string[] commandParts)
        {
            var resourceType = "Videre.Core.Models.Localization";
            var includeAudits = true;
            if (commandParts.Length > 1)
                resourceType = commandParts[1];
            if (commandParts.Length > 2)
                includeAudits = bool.Parse(commandParts[2]);

            var cr = RepositoryService.GetResources(resourceType, includeAudits);
            var crSave = RepositoryService.SaveResources(cr.Data);
            Console.WriteLine(crSave.Data.ToJson(true));
        }

        private static void DoObtainLock(string[] commandParts)
        {
            var source = Environment.MachineName;
            var ns = "My Namespace";
            if (commandParts.Length > 1)
                source = commandParts[1];
            if (commandParts.Length > 2)
                ns = commandParts[2];

            var cr = RepositoryService.ObtainLock(source, ns);
            Console.WriteLine(cr.Data.ToJson(true));
        }
        private static void DoRemoveLock(string[] commandParts)
        {
            var source = Environment.MachineName;
            var ns = "My Namespace";
            if (commandParts.Length > 1)
                source = commandParts[1];
            if (commandParts.Length > 2)
                ns = commandParts[2];

            var cr = RepositoryService.RemoveLock(source, ns);
            Console.WriteLine(cr.Data.ToJson(true));
        }

        private static void recordMessage(string message)
        {
            Console.WriteLine(message);
        }

        private static void recordMessage(string message, params object[] args)
        {
            recordMessage(string.Format(message, args));
        }

        public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            if (assemblyName.Name != args.Name)
                return Assembly.LoadWithPartialName(assemblyName.Name);
            return null;
        }

        private static void initializeCache()
        {
            var clientId = string.Format("{0}:{1}", Environment.MachineName, System.AppDomain.CurrentDomain.FriendlyName);
            var cacheConnection = "";
            var cacheName = "";
            var cacheServer = "";

            Console.WriteLine("Enter CacheType:");
            Console.WriteLine("0)  InMemory (default)");
            Console.WriteLine("1)  Local Redis");
            Console.WriteLine("2)  SJCACHE Redis");
            Console.WriteLine("3)  None");
            var cacheTypeNum = Console.ReadLine();
            if (cacheTypeNum == "1" || cacheTypeNum == "2")
            {
                cacheName = "Redis";
                cacheServer = cacheTypeNum == "1" ? "127.0.0.1" : "10.192.1.95:6379";
                //cacheConnection = string.Format("{{'cacheType': 'CodeEndeavors.Distributed.Cache.Client.Redis.RedisCache', 'clientId': '{0}', 'server': '{1},syncTimeout=10000', 'absoluteExpiration': '\"00:10:00\"'}}", clientId, cacheServer);
                cacheConnection = string.Format("{{'cacheType': 'CodeEndeavors.Distributed.Cache.Client.Redis.RedisCache', 'clientId': '{0}', 'server': '{1},syncTimeout=10000'}}", clientId, cacheServer);
            }
            else if (cacheTypeNum == "3") { }
            else
            {
                cacheName = "InMemory";
                cacheConnection = string.Format("{{'cacheType': 'CodeEndeavors.Distributed.Cache.Client.InMemory.InMemoryCache', 'clientId': '{0}', 'absoluteExpiration': '\"00:10:00\"' }}", clientId);
            }

            recordMessage("ConfigureCache({0}, {1})", cacheName, cacheConnection);
            RepositoryService.ConfigureCache(cacheName, cacheConnection);
        }

    }
}
