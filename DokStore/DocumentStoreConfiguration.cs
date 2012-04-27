using System;
using System.Collections.Generic;
using System.Reflection;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using DokStore.Mapping;

namespace DokStore
{
    public class DocumentStoreConfiguration
    {
        public Configuration NHConfiguration { get; private set; }

        private ModelMapper _modelMapper;
        internal IDictionary<Type, Type> EntityToCollectionMap;

        public DocumentStoreConfiguration() : 
            this (new Configuration().SessionFactoryName("DokStore") )
        {
        }

        public DocumentStoreConfiguration(Configuration configuration)
        {
            NHConfiguration = configuration;

            _modelMapper = new ModelMapper();
            EntityToCollectionMap = new Dictionary<Type, Type>(20);
        }


        public void AddCollection<TCollection, TEntity>(Action<IDocumentCollectionMapper<TCollection, TEntity>> mappingAction)
            where TCollection : DocumentCollection<TEntity>
            where TEntity : class
        {
            if (_modelMapper == null)
                throw new Exception("Mapping has already been compiled, no further collections can be added!");
            
            //Default collection mapping
            _modelMapper.Class<TCollection>(ca => 
            {
                ca.Lazy(false);
                ca.Property(p => p.JsonDocument, pm => pm.Length(10000));
            });

            //Perform custom mapping
            var collectionMapper = new DocumentCollectionMapper<TCollection, TEntity>(_modelMapper);
            mappingAction.Invoke(collectionMapper);

            //Add to collection map
            EntityToCollectionMap.Add(typeof(TEntity), typeof(TCollection));

            if (collectionMapper.GetIdValue != null)
                SetGetIdValue<TCollection, TEntity>(collectionMapper.GetIdValue);

            if (collectionMapper.UpdateIdAction != null)
                SetUpdateIdAction(collectionMapper.UpdateIdAction);
        }

        private void SetGetIdValue<TCollection, TEntity>(Func<TCollection, object> idFunction)
            where TCollection : DocumentCollection<TEntity>
            where TEntity : class
        {
            //Find property info
            var propInfo = typeof(TCollection).GetProperty("GetIdValue", BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);

            if (propInfo != null) propInfo.SetValue(null, idFunction);
        }

        private void SetUpdateIdAction<TCollection, TEntity>(Action<TCollection, TEntity> action)
            where TCollection : DocumentCollection<TEntity>
            where TEntity : class
        {
            //Find property info
            var propInfo = typeof(TCollection).GetProperty("UpdateIdAction", BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            
            if (propInfo != null) propInfo.SetValue(null, action);
        }


        public void CompileMappings()
        {
            if (_modelMapper == null) return;   //Already compiled

            var mapping = _modelMapper.CompileMappingForAllExplicitlyAddedEntities();

            NHConfiguration.AddMapping(mapping);

            _modelMapper = null;
        }


        public IDocumentStore BuildDocumentStore()
        {
            CompileMappings();

            return new DocumentStore(this);
        }
    }
}
