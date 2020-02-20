using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace Kugar.Core.Web.Core3.Demo.Controllers
{
    /// <summary>
    /// 合并多个IContractResolver,,并只返回第一个返回非null的Contract,如果所有列表中的ContractResolver都返回null,则调用DefaultContractResolver返回默认的JsonContract
    /// </summary>
    public class CompositeContractResolver : IContractResolver, IEnumerable<IContractResolver>
    {
        private readonly IList<IContractResolver> _contractResolvers = new List<IContractResolver>();
        private static DefaultContractResolver _defaultResolver = new DefaultContractResolver();
        private ConcurrentDictionary<Type, JsonContract> _cacheContractResolvers=new ConcurrentDictionary<Type, JsonContract>();

        /// <summary>
        /// 返回列表中第一个返回非null的Contract,如果所有列表中的ContractResolver都返回null,则调用DefaultContractResolver返回默认的JsonContract
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public JsonContract ResolveContract(Type type)
        {
            return _cacheContractResolvers.GetOrAdd(type, m =>
            {
                for (int i = 0; i < _contractResolvers.Count; i++)
                {
                    var contact = _contractResolvers[i].ResolveContract(type);

                    if (contact != null)
                    {
                        return contact;
                    }
                }

                return _defaultResolver.ResolveContract(type);
            });
        }

        public void Add(IContractResolver contractResolver)
        {
            if (contractResolver == null) return;

            _contractResolvers.Add(contractResolver);
        }

        public void Insert(int index, IContractResolver contractResolver)
        {
            _contractResolvers.Insert(index,contractResolver);
        }

        public IEnumerator<IContractResolver> GetEnumerator()
        {
            return _contractResolvers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}