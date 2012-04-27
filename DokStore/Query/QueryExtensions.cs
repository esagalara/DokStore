using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate;

namespace DokStore.Query
{
    public static class QueryExtensions
    {
        /// <summary>
        /// Return entities from a QueryOver
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public static IEnumerable<TEntity> DocumentList<TCollection, TEntity>(this IQueryOver<TCollection, TCollection> query)
            where TCollection : DocumentCollection<TEntity>
            where TEntity : class
        {
            //Only select json document
            query.Select(NHibernate.Criterion.Projections.Property("JsonDocument"));

            var jsonList = query.List<string>();

            return UnpackEntityFromJson<TEntity>(jsonList);
        }

        /// <summary>
        /// Return entities from a HQL query
        /// </summary>
        /// <typeparam name="TCollection"></typeparam>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public static IEnumerable<TEntity> DocumentList<TCollection, TEntity>(this IQuery query)
            where TCollection : DocumentCollection<TEntity>
            where TEntity : class
        {
            //query.SetResultTransformer(NHibernate.Transform.Transformers.AliasToBean(typeof(TCollection)));
            var list = query.List<TCollection>();

            var jsonList = list.Select(c => c.JsonDocument);
            return UnpackEntityFromJson<TEntity>(jsonList);
        }

        private static IEnumerable<TEntity> UnpackEntityFromJson<TEntity>(IEnumerable<string> jsonDocuments) where TEntity : class
        {
            foreach (var json in jsonDocuments)
            {
                yield return DocumentSession.Unpack<TEntity>(json);
            }
        }
    }
}
