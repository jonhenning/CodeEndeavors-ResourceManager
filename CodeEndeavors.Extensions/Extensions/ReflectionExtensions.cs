using System;
using System.IO;
using System.Web;
using System.Web.Caching;
using System.Collections.Concurrent;
using System.Reflection;
using System.Management.Instrumentation;

namespace CodeEndeavors.Extensions
{
    public static class ReflectionExtensions
    {
        private static readonly ConcurrentDictionary<string, Type> m_providers = new ConcurrentDictionary<string, Type>();
        public static T GetInstance<T>(this string typeName, string assemblyPath = null)
        {
            var type = typeName.ToType();
            //if (string.IsNullOrWhiteSpace(typeName))
            //    throw new ArgumentException("The parameter cannot be null.", "typeName");

            object obj;
            //if (m_providers.TryGetValue(typeName, out obj))
            //    return obj.ToType<T>();

            //if (!string.IsNullOrWhiteSpace(assemblyPath))
            //    Assembly.LoadFrom(assemblyPath);

            //var type = Type.GetType(typeName);
            //if (type == null)
            //    throw new TypeLoadException(string.Format("Unable to load type: {0}", typeName));

            obj = Activator.CreateInstance(type);// as IWidgetContentProvider;
            if (obj == null)
                throw new InstanceNotFoundException(string.Format("Unable to create a valid instance of {0} from type: {1}", typeof(T).ToString(), typeName));

            return obj.ToType<T>();
        }

        public static Type ToType(this string typeName, string assemblyPath = null)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("The parameter cannot be null.", "typeName");

            Type type;
            if (m_providers.TryGetValue(typeName, out type))
                return type;

            if (!string.IsNullOrWhiteSpace(assemblyPath))
                Assembly.LoadFrom(assemblyPath);

            type = Type.GetType(typeName);
            if (type == null)
                throw new TypeLoadException(string.Format("Unable to load type: {0}", typeName));

            m_providers[typeName] = type;
            return type;
        }

    }

}