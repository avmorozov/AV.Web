﻿// -----------------------------------------------------------------------
// <copyright file="EntityRepositary.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Database
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Entity;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using AV.Models;

    /// <summary>
    /// </summary>
    public class EntityRepositary<TEntity> : IRepositary<TEntity>
        where TEntity : class, IEntity, new()
    {
        /// <summary>
        ///   Database context
        /// </summary>
        private readonly DbContext _dbContext;

        /// <summary>
        ///   Database set of entities
        /// </summary>
        private readonly dynamic _entitySet;

        /// <summary>
        ///   Query context
        /// </summary>
        private readonly IQueryable<TEntity> _queryContext;

        /// <summary>
        ///   Initializes a new instance of the <see cref="EntityRepositary&lt;TEntity&gt;" /> class.
        /// </summary>
        /// <param name="dbContext"> The db context. </param>
        public EntityRepositary(DbContext dbContext)
        {
            this._dbContext = dbContext;
            var dbSetProperty = GetDbSetProperty(dbContext);
            this._entitySet = dbSetProperty.GetValue(dbContext, null);
            this._queryContext = Queryable.OfType<TEntity>(this._entitySet);
        }

        /// <summary>
        ///   DbSet property info
        /// </summary>
        /// <param name="dbContext"> </param>
        /// <returns> </returns>
        private static PropertyInfo GetDbSetProperty(DbContext dbContext)
        {
            var possibleTypes = GetPossibleTypes();
            var dbsetType = typeof(IDbSet<>);
            foreach (var type in possibleTypes)
            {
                var propertyType = dbsetType.MakeGenericType(new[] { type });
                var dbSetProperty = (from property in dbContext.GetType().GetProperties()
                                     where propertyType.IsAssignableFrom(property.PropertyType)
                                     select property).SingleOrDefault();
                if (dbSetProperty != null)
                {
                    return dbSetProperty;
                }
            }
            throw new EntityRepositaryException(
                String.Format(
                    CultureInfo.InvariantCulture,
                    "There is no property with type {0} in database context",
                    typeof(IDbSet<TEntity>).FullName));
        }

        /// <summary>
        ///   Possible type parameters for DbSet
        /// </summary>
        /// <returns> </returns>
        private static IList<Type> GetPossibleTypes()
        {
            var possibleTypes = new List<Type>();
            var typeDef = typeof(TEntity);
            while (typeDef != typeof(object) && typeDef != null)
            {
                possibleTypes.Add(typeDef);
                typeDef = typeDef.BaseType;
            }
            return possibleTypes;
        }

        /// <summary>
        ///   Gets the type of the element(s) that are returned when the expression tree associated with this instance of <see
        ///    cref="T:System.Linq.IQueryable" /> is executed.
        /// </summary>
        /// <value> </value>
        /// <returns> A <see cref="T:System.Type" /> that represents the type of the element(s) that are returned when the expression tree associated with this object is executed. </returns>
        public Type ElementType
        {
            get
            {
                return this._queryContext.ElementType;
            }
        }

        /// <summary>
        ///   Gets the expression tree that is associated with the instance of <see cref="T:System.Linq.IQueryable" /> .
        /// </summary>
        /// <value> </value>
        /// <returns> The <see cref="T:System.Linq.Expressions.Expression" /> that is associated with this instance of <see
        ///    cref="T:System.Linq.IQueryable" /> . </returns>
        public System.Linq.Expressions.Expression Expression
        {
            get
            {
                return this._queryContext.Expression;
            }
        }

        /// <summary>
        ///   Gets the query provider that is associated with this data source.
        /// </summary>
        /// <value> </value>
        /// <returns> The <see cref="T:System.Linq.IQueryProvider" /> that is associated with this data source. </returns>
        public IQueryProvider Provider
        {
            get
            {
                return this._queryContext.Provider;
            }
        }

        /// <summary>
        /// Dispose pattern 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern 
        /// </summary>
        /// <param name="disposing">if object should be disposed</param>
        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                _dbContext.Dispose();
            }
        }

        /// <summary>
        /// Create new instance of entity
        /// </summary>
        /// <returns>Entity instance</returns>
        public TEntity Create()
        {
            TEntity entity = _entitySet.Create<TEntity>();
            return entity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public void Update(TEntity obj)
        {
            _dbContext.Entry(obj).Reload();
        }

        /// <summary>
        /// Delete entity from database
        /// </summary>
        /// <param name="obj"></param>
        public void Remove(TEntity obj)
        {
            if (_dbContext.Entry(obj).State != EntityState.Detached)
                _entitySet.Remove(obj);
            _dbContext.SaveChanges();
        }

        /// <summary>
        /// Saves the specified obj.
        /// </summary>
        /// <param name="obj">The object to save.</param>
        public void Save(TEntity obj)
        {
            if (_dbContext.Entry(obj).State == System.Data.EntityState.Detached)
                _entitySet.Add(obj);
            _dbContext.SaveChanges();
        }

        /// <summary>
        ///   Gets the enumerator.
        /// </summary>
        /// <returns> Enumerator to iterate </returns>
        public IEnumerator<TEntity> GetEnumerator()
        {
            return this._queryContext.GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns> An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection. </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._queryContext.GetEnumerator();
        }
    }
}