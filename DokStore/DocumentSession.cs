using System;
using System.Collections.Generic;
using NHibernate;
using Newtonsoft.Json;

namespace DokStore
{
    public interface IDocumentSession : IDisposable
    {
        ISession NHSession { get; }

        /// <summary>
        /// Get entity by Id
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        TEntity GetById<TEntity>(object id) where TEntity : class;

        /// <summary>
        /// Store an new or existing entity as a document
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        void Store<TEntity>(TEntity entity) where TEntity : class;

        /// <summary>
        /// Perform several operations in a transaction
        /// </summary>
        /// <param name="batch">Batch of entity updates</param>
        void Batch(Action<IDocumentSession> batch);

        /// <summary>
        /// Returns QueryOver for collection
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <returns></returns>
        IQueryOver<TCollection, TCollection> Query<TCollection>() where TCollection : class;

        /// <summary>
        /// HQL query
        /// </summary>
        /// <param name="hql"></param>
        /// <returns></returns>
        IQuery Query(string hql);
    }

    internal class DocumentSession : IDocumentSession
    {
        public ISession NHSession { get; private set; }

        private IDictionary<Type, Type> _entityToCollectionMap;

        internal DocumentSession(ISession session, IDictionary<Type, Type> entityToCollectionMap)
        {
            NHSession = session;

            _entityToCollectionMap = entityToCollectionMap;
        }

        private Type GetCollectionType(Type entityType)
        {
            if (!_entityToCollectionMap.ContainsKey(entityType))
                throw new ArgumentException("Unknown entity type", "TEntity");

            return _entityToCollectionMap[entityType];
        }


        public TEntity GetById<TEntity>(object id) where TEntity : class
        {
            var collectionType = GetCollectionType(typeof(TEntity));

            var collection = NHSession.Get(collectionType.Name, id) as DocumentCollection<TEntity>;

            //Deserialize from json
            return Unpack<TEntity>(collection.JsonDocument);
        }


        public void Store<TEntity>(TEntity entity) where TEntity : class
        {
            //Instantiate collection class
            var collectionType = GetCollectionType(typeof(TEntity));
            var collection = Activator.CreateInstance(collectionType);

            //Force update inside transaction
            if (NHSession.Transaction.IsActive)
                Update(collection, entity);
            else
                Batch(s => Update(collection, entity));
        }

        private void Update<TEntity>(object collection, TEntity entity) where TEntity : class
        {
            var genericCollection = collection as DocumentCollection<TEntity>;  //so we can access properties without activator 

            genericCollection.BeforeSave(entity);

            object collId = genericCollection.GetId();
            if (collId == null)
            {
                //First time saved, create collection to generate an Id 
                //TODO: only use for db-generated IDs
                NHSession.Save(collection);
                genericCollection.UpdateEntityId(entity);
            }

            //Serialize to json
            genericCollection.JsonDocument = Pack(entity);

            //Save to db
            NHSession.Update(collection);
        }

        public void Batch(Action<IDocumentSession> batch)
        {
            using (var tx = NHSession.BeginTransaction())
            {
                try
                {
                    batch.Invoke(this);
                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    throw ex;
                }
            }
        }


        public IQueryOver<TCollection, TCollection> Query<TCollection>() where TCollection : class
        {
            return NHSession.QueryOver<TCollection>();
        }

        public IQuery Query(string hql)
        {
            return NHSession.CreateQuery(hql);
        }


        internal static string Pack(object entity)
        {
            return JsonConvert.SerializeObject(entity);
        }

        internal static TEntity Unpack<TEntity>(string json) where TEntity : class
        {
            return JsonConvert.DeserializeObject<TEntity>(json);
        }


        public void Dispose()
        {
            NHSession.Dispose();
        }
    }
}
