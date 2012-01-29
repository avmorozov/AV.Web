// -----------------------------------------------------------------------
// <copyright file="FakeRepositary.cs" company="ILabs NoWare">
//   (c) Alexander Morozov, 2012
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Practices.ServiceLocation;

namespace AV.Models
{
    /// <summary>
    ///   Fake repositary based on list in memory (for testing purposes)
    /// </summary>
    /// <typeparam name="TEntity"> The type of the entity. </typeparam>
    public class FakeRepositary<TEntity> : IRepositary<TEntity>
        where TEntity : class, IEntity
    {
        /// <summary>
        ///   Finalizer for current class
        /// </summary>
        ~FakeRepositary()
        {
            Dispose(false);
        }

        #region Constants and Fields

        private const BindingFlags PersistedPropertiesFlags =
            BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance;

        /// <summary>
        ///   Memory buffer
        /// </summary>
        private readonly List<TEntity> _memoryBuffer = new List<TEntity>();

        private HashSet<TEntity> _saveTransaction;

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets the memory buffer.
        /// </summary>
        /// <value> The memory buffer. </value>
        public ReadOnlyCollection<TEntity> MemoryBuffer
        {
            get { return this._memoryBuffer.AsReadOnly(); }
        }

        /// <summary>
        ///   Gets the type of the element(s) that are returned when the expression tree associated with this instance of <see
        ///    cref="T:System.Linq.IQueryable" /> is executed.
        /// </summary>
        /// <value> </value>
        /// <returns> A <see cref="T:System.Type" /> that represents the type of the element(s) that are returned when the expression tree associated with this object is executed. </returns>
        public Type ElementType
        {
            get { return this._memoryBuffer.AsQueryable().ElementType; }
        }

        /// <summary>
        ///   Gets the expression tree that is associated with the instance of <see cref="T:System.Linq.IQueryable" /> .
        /// </summary>
        /// <value> </value>
        /// <returns> The <see cref="T:System.Linq.Expressions.Expression" /> that is associated with this instance of <see
        ///    cref="T:System.Linq.IQueryable" /> . </returns>
        public Expression Expression
        {
            get { return this._memoryBuffer.AsQueryable().Expression; }
        }

        /// <summary>
        ///   Gets the query provider that is associated with this data source.
        /// </summary>
        /// <value> </value>
        /// <returns> The <see cref="T:System.Linq.IQueryProvider" /> that is associated with this data source. </returns>
        public IQueryProvider Provider
        {
            get { return this._memoryBuffer.AsQueryable().Provider; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Gets the enumerator.
        /// </summary>
        /// <returns> . An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection. </returns>
        public IEnumerator<TEntity> GetEnumerator()
        {
            return this._memoryBuffer.GetEnumerator();
        }

        /// <summary>
        ///   Create a new entity instance
        /// </summary>
        /// <returns> Create entity instance </returns>
        public TEntity Create()
        {
            return Activator.CreateInstance<TEntity>();
        }

        /// <summary>
        ///   Deletes the specified entity.
        /// </summary>
        /// <param name="obj"> The entity. </param>
        public void Remove(TEntity obj)
        {
            if (this._memoryBuffer.Contains(obj))
                this._memoryBuffer.Remove(obj);
        }

        /// <summary>
        ///   Saves the specified entity.
        /// </summary>
        /// <param name="obj"> The entity. </param>
        public void Save(TEntity obj)
        {
            if (obj == null)
                throw new FakeRepositaryException("Can't save null obects");

            bool saveTransactionBegins = false;
            if (this._saveTransaction == null)
            {
                saveTransactionBegins = true;
                this._saveTransaction = new HashSet<TEntity>();
            }

            try
            {
                if (this._saveTransaction.Contains(obj))
                    return;

                this._saveTransaction.Add(obj);

                if (!this._memoryBuffer.Contains(obj))
                {
                    obj.Id = this._memoryBuffer.Count > 0 ? this._memoryBuffer.Max(x => x.Id) + 1 : 1;
                    this._memoryBuffer.Add(obj);
                }

                SaveProperties(obj);
            }
            finally
            {
                if (saveTransactionBegins)
                    this._saveTransaction = null;
            }
        }

        /// <summary>
        ///   Updates data using entity key
        /// </summary>
        /// <param name="obj"> Object to update </param>
        public void Update(TEntity obj)
        {
            if (obj == null)
                throw new FakeRepositaryException("Can't update null objects");

            TEntity repositaryEntity = this.SingleOrDefault(x => x.Id == obj.Id);
            if (repositaryEntity == null)
                throw new FakeRepositaryException("Entity with key not found.");

            foreach (PropertyInfo property in typeof (TEntity).GetProperties(PersistedPropertiesFlags))
            {
                object value = property.GetValue(repositaryEntity, null);
                property.SetValue(obj, value, null);
            }
        }

        /// <summary>
        ///   Clear memory buffer.
        /// </summary>
        public void Clear()
        {
            this._memoryBuffer.Clear();
        }

        /// <summary>
        ///   Implementatio of disposing pattern
        /// </summary>
        /// <param name="disposing"> </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                this._memoryBuffer.Clear();
        }

        /// <summary>
        ///   Saves the specified entity collection.
        /// </summary>
        /// <param name="obj"> The entity collection. </param>
        public void Save(IEnumerable<TEntity> obj)
        {
            foreach (TEntity entity in obj)
                Save(entity);
        }

        #endregion

        #region Explicit Interface Methods

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns> An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection. </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._memoryBuffer.GetEnumerator();
        }

        #endregion

        #region Methods

        /// <summary>
        ///   Saves properties
        /// </summary>
        /// <param name="obj"> Object instance </param>
        private void SaveProperties(object obj)
        {
            Type type = obj.GetType();
            foreach (PropertyInfo propertyInfo in type.GetProperties(PersistedPropertiesFlags))
            {
                if (!propertyInfo.GetIndexParameters().Any())
                {
                    object propertyValue = propertyInfo.GetValue(obj, null);
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
                Type repositaryType = typeof (IRepositary<>).MakeGenericType(propertyType);
                MethodInfo repositaryMethod = repositaryType.GetMethod("Save");
                object repositary = ServiceLocator.Current.GetInstance(repositaryType);
                if (repositary != null)
                    repositaryMethod.Invoke(repositary, new[] {propertyValue});
            }
            // many assoociation
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof (ICollection<>))
            {
                var childCollection = (IEnumerable) propertyValue;
                if (childCollection != null)
                {
                    foreach (object subentity in childCollection)
                        SaveProperty(propertyType.GetGenericArguments().First(), subentity);
                }
            }
        }

        #endregion
    }
}