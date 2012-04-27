using System;
using System.Collections.Generic;
using NHibernate;

namespace DokStore
{
    public interface IDocumentStore : IDisposable
    {
        ISessionFactory NHSessionFactory { get; }
        IDocumentSession OpenSession();
    }

    internal class DocumentStore : IDocumentStore
    {
        public ISessionFactory NHSessionFactory { get; private set; }

        private IDictionary<Type, Type> _entityToCollectionMap;

        internal DocumentStore(DocumentStoreConfiguration configuration)
        {
            NHSessionFactory = configuration.NHConfiguration.BuildSessionFactory();
            _entityToCollectionMap = configuration.EntityToCollectionMap;
        }

        public IDocumentSession OpenSession()
        {
            return new DocumentSession(NHSessionFactory.OpenSession(), _entityToCollectionMap);
        }

        public void Dispose()
        {
            if (NHSessionFactory != null)
                NHSessionFactory.Dispose();
        }
    }
}
