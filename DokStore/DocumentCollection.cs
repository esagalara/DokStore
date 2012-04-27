using System;
using System.Collections.Generic;
using NHibernate.Mapping.ByCode;

namespace DokStore
{
    public abstract class DocumentCollection<TEntity> where TEntity : class
    {
        /// <summary>
        /// The entity saved as JSON
        /// </summary>
        public virtual string JsonDocument { get; set; }
      

        /// <summary>
        /// Update the index properties before the entity is saved
        /// </summary>
        /// <param name="entity"></param>
        protected abstract void UpdateIndices(TEntity entity);

        public virtual void BeforeSave(TEntity entity)
        {
            UpdateIndices(entity);
        }

        public virtual object GetId()
        {
            return GetIdValue(this);
        }

        public virtual void UpdateEntityId(TEntity entity)
        {
            if (UpdateIdAction != null) 
                UpdateIdAction.Invoke(this, entity);
        }

        internal static Func<DocumentCollection<TEntity>, object> GetIdValue { get; set; }
        internal static Action<DocumentCollection<TEntity>, TEntity> UpdateIdAction { get; set; }
    }
}
