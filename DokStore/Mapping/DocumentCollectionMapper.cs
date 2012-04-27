using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NHibernate.Mapping.ByCode;

namespace DokStore.Mapping
{
    public interface IDocumentCollectionMapper<TCollection, TEntity>
        where TCollection : DocumentCollection<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Define Id of the collection
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="idProperty"></param>
        /// <param name="idMapper"></param>
        void Id<TProperty>(Expression<Func<TCollection, TProperty>> idProperty, Action<IIdMapper> idMapper = null) where TProperty : struct;

        /// <summary>
        /// Define index property
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="property"></param>
        /// <param name="propertyMapper"></param>
        void Index<TProperty>(Expression<Func<TCollection, TProperty>> property, Action<IPropertyMapper> propertyMapper = null);

        /// <summary>
        /// Reference another collection
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="referenceProperty"></param>
        void ReferenceIndex<TProperty>(Expression<Func<TCollection, TProperty>> referenceProperty) where TProperty : class;
    }


    internal class DocumentCollectionMapper<TCollection, TEntity> : IDocumentCollectionMapper<TCollection, TEntity>
        where TCollection : DocumentCollection<TEntity>
        where TEntity : class
    {
        private ModelMapper _mapper;

        internal Func<DocumentCollection<TEntity>, object> GetIdValue { get; set; }
        internal Action<DocumentCollection<TEntity>, TEntity> UpdateIdAction { get; set; }        

        public DocumentCollectionMapper(ModelMapper mapper)
        {
            _mapper = mapper;
        }

        public void Id<TProperty>(Expression<Func<TCollection, TProperty>> idProperty, Action<IIdMapper> idMapper = null) where TProperty : struct
        {
            //Map Id with Nhibernate
            if (idMapper != null)
                _mapper.Class<TCollection>(ca => ca.Id(idProperty, idMapper));
            else
                _mapper.Class<TCollection>(ca => ca.Id(idProperty));            

            //Collection Id property
            var collectionIdProperty = TypeExtensions.DecodeMemberAccessExpression(idProperty) as PropertyInfo;
            if (collectionIdProperty == null || !collectionIdProperty.CanWrite) return;

            //Create function for retrieving instance Id
            GetIdValue = (c) =>
            {
                var value = (TProperty)collectionIdProperty.GetValue(c);
                if (default(TProperty).Equals(value)) return null;                
                return value;
            };

            //Find corresponding Entity Id property
            var entityIdProperty = typeof(TEntity).GetProperty(collectionIdProperty.Name);
            if (entityIdProperty == null || !entityIdProperty.CanWrite) return;
            if (entityIdProperty.PropertyType != collectionIdProperty.PropertyType) return;

            //Create action that copies Id from Collection to Entity
            UpdateIdAction = (c, e) =>
            {
                var id = (TProperty) collectionIdProperty.GetValue(c);
                entityIdProperty.SetValue(e, id, null);
            };
        }

        public void Index<TProperty>(Expression<Func<TCollection, TProperty>> property, Action<IPropertyMapper> propertyMapper)
        {
            //Map index as NHIbernate property
            if (propertyMapper != null)
                _mapper.Class<TCollection>(ca => ca.Property(property, propertyMapper));
            else
                _mapper.Class<TCollection>(ca => ca.Property(property));
        }


        public void ReferenceIndex<TProperty>(Expression<Func<TCollection, TProperty>> referenceProperty) where TProperty : class
        {
            _mapper.Class<TCollection>(ca => ca.ManyToOne(referenceProperty, map => map.Lazy(LazyRelation.NoLazy)));
        }
    }
}
