// -----------------------------------------------------------------------
// <copyright file="FakeRepositary.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Models.Repositary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using Microsoft.Practices.ServiceLocation;

    /// <summary>
    ///   Fake repositary based on list in memory (for testing purposes)
    /// </summary>
    /// <typeparam name="TEntity"> The type of the entity. </typeparam>
    public class FakeRepositary<TEntity> : IRepositary<TEntity>
        where TEntity : class, IEntity
    {
        #region Constants and Fields

        private const BindingFlags PersistedPropertiesFlags =
            BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance;

        /// <summary>
        ///   Memory buffer
        /// </summary>
        private readonly List<TEntity> memoryBuffer = new List<TEntity>();

        private HashSet<TEntity> saveTransaction;

        #endregion

        #region Public Properties

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
                return this.memoryBuffer.AsQueryable().ElementType;
            }
        }

        /// <summary>
        ///   Gets the expression tree that is associated with the instance of <see cref="T:System.Linq.IQueryable" /> .
        /// </summary>
        /// <value> </value>
        /// <returns> The <see cref="T:System.Linq.Expressions.Expression" /> that is associated with this instance of <see
        ///    cref="T:System.Linq.IQueryable" /> . </returns>
        public Expression Expression
        {
            get
            {
                return this.memoryBuffer.AsQueryable().Expression;
            }
        }

        /// <summary>
        ///   Gets the memory buffer.
        /// </summary>
        /// <value> The memory buffer. </value>
        public List<TEntity> MemoryBuffer
        {
            get
            {
                return this.memoryBuffer;
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
                return this.memoryBuffer.AsQueryable().Provider;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Clear memory buffer.
        /// </summary>
        public void Clear()
        {
            this.memoryBuffer.Clear();
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.memoryBuffer.Clear();
        }

        /// <summary>
        ///   Gets the enumerator.
        /// </summary>
        /// <returns> . An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection. </returns>
        public IEnumerator<TEntity> GetEnumerator()
        {
            return this.memoryBuffer.GetEnumerator();
        }

        /// <summary>
        ///   Create a new entity instance
        /// </summary>
        /// <returns> New entity instance </returns>
        public TEntity New()
        {
            return Activator.CreateInstance<TEntity>();
        }

        /// <summary>
        ///   Deletes the specified entity.
        /// </summary>
        /// <param name="obj"> The entity. </param>
        public void Remove(TEntity obj)
        {
            if (this.memoryBuffer.Contains(obj))
                this.memoryBuffer.Remove(obj);
        }

        /// <summary>
        ///   Saves the specified entity.
        /// </summary>
        /// <param name="obj"> The entity. </param>
        public void Save(TEntity obj)
        {
            if (obj == null)
                throw new ApplicationException("Can't save null obects");

            var saveTransactionBegins = false;
            if (this.saveTransaction == null)
            {
                saveTransactionBegins = true;
                this.saveTransaction = new HashSet<TEntity>();
            }

            try
            {
                if (this.saveTransaction.Contains(obj))
                    return;

                this.saveTransaction.Add(obj);

                if (!this.memoryBuffer.Contains(obj))
                {
                    obj.Id = memoryBuffer.Count > 0 ? memoryBuffer.Max(x => x.Id) + 1 : 1;
                    this.memoryBuffer.Add(obj);
                }

                SaveProperties(obj);
            }
            finally
            {
                if (saveTransactionBegins)
                    this.saveTransaction = null;
            }
        }

        /// <summary>
        ///   Saves the specified entity collection.
        /// </summary>
        /// <param name="obj"> The entity collection. </param>
        public void Save(IEnumerable<TEntity> obj)
        {
            foreach (var entity in obj)
                Save(entity);
        }

        /// <summary>
        ///   Updates data using entity key
        /// </summary>
        /// <param name="obj"> Object to update </param>
        public void Update(TEntity obj)
        {
            if (obj == null)
                throw new ApplicationException("Can't update null objects");

            var repositaryEntity = this.SingleOrDefault(x => x.Id == obj.Id);
            if (repositaryEntity == null)
                throw new ApplicationException("Entity with key not found.");

            foreach (var property in typeof(TEntity).GetProperties(PersistedPropertiesFlags))
            {
                var value = property.GetValue(repositaryEntity, null);
                property.SetValue(obj, value, null);
            }
        }

        #endregion

        #region Explicit Interface Methods

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns> An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection. </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.memoryBuffer.GetEnumerator();
        }

        #endregion

        #region Methods

        /// <summary>
        ///   Saves properties
        /// </summary>
        /// <param name="obj"> Object instance </param>
        private void SaveProperties(object obj)
        {
            var type = obj.GetType();
            foreach (var propertyInfo in type.GetProperties(PersistedPropertiesFlags))
            {
                if (!propertyInfo.GetIndexParameters().Any())
                {
                    var propertyValue = propertyInfo.GetValue(obj, null);
                    SaveProperty(propertyInfo.PropertyType, propertyValue);
                }
            }
        }

        /// <summary>
        ///   Saves property value in repositary structure
        /// </summary>
        /// <param name="propertyType"> The property type. </param>
        /// <param name="propertyValue"> The property value. </param>
        private void SaveProperty(Type propertyType, object propertyValue)
        {
            // one association
            if (propertyValue != null && propertyType.GetProperty("Id") != null)
            {
                var repositaryType = typeof(IRepositary<>).MakeGenericType(propertyType);
                var repositaryMethod = repositaryType.GetMethod("Save");
                var repositary = ServiceLocator.Current.GetInstance(repositaryType);
                if (repositary != null)
                    repositaryMethod.Invoke(repositary, new[] { propertyValue });
            }
            // many assoociation
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
            {
                var childCollection = (IEnumerable)propertyValue;
                if (childCollection != null)
                {
                    foreach (var subentity in childCollection)
                        SaveProperty(propertyType.GetGenericArguments().First(), subentity);
                }
            }
        }

        #endregion
    }
}