// -----------------------------------------------------------------------
// <copyright file="FakeRepositary.cs" company="AV">
// Fake repositary based on list in memory (for testing purposes)
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Models.Repositary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    using Microsoft.Practices.ServiceLocation;

    /// <summary>
    /// Fake repositary based on list in memory (for testing purposes)
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class FakeRepositary<TEntity> : IRepositary<TEntity> where TEntity : class
    {
        /// <summary>
        /// Memory buffer
        /// </summary>
        private readonly List<TEntity> memoryBuffer = new List<TEntity>();

        private HashSet<TEntity> saveTransaction = null;

        /// <summary>
        /// Gets the memory buffer.
        /// </summary>
        /// <value>The memory buffer.</value>
        public List<TEntity> MemoryBuffer
        {
            get
            {
                return this.memoryBuffer;
            }
        }

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree associated with this instance of <see cref="T:System.Linq.IQueryable"/> is executed.
        /// </summary>
        /// <value></value>
        /// <returns>A <see cref="T:System.Type"/> that represents the type of the element(s) that are returned when the expression tree associated with this object is executed.</returns>
        public Type ElementType
        {
            get { return this.memoryBuffer.AsQueryable().ElementType; }
        }

        /// <summary>
        /// Gets the expression tree that is associated with the instance of <see cref="T:System.Linq.IQueryable"/>.
        /// </summary>
        /// <value></value>
        /// <returns>The <see cref="T:System.Linq.Expressions.Expression"/> that is associated with this instance of <see cref="T:System.Linq.IQueryable"/>.</returns>
        public Expression Expression
        {
            get { return this.memoryBuffer.AsQueryable().Expression; }
        }

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        /// <value></value>
        /// <returns>The <see cref="T:System.Linq.IQueryProvider"/> that is associated with this data source.</returns>
        public IQueryProvider Provider
        {
            get { return this.memoryBuffer.AsQueryable().Provider; }
        }

        /// <summary>
        /// Create a new entity instance
        /// </summary>
        /// <returns>
        /// New entity instance
        /// </returns>
        public TEntity New()
        {
            return Activator.CreateInstance<TEntity>();
        }

        /// <summary>
        /// Saves the specified entity.
        /// </summary>
        /// <param name="obj">The entity.</param>
        public void Save(TEntity obj)
        {
            if (obj == null)
                throw new ApplicationException("Can't save null obects");

            var saveTransactionBegins = false;
            if (saveTransaction == null)
            {
                saveTransactionBegins = true;
                saveTransaction = new HashSet<TEntity>();
            }

            try
            {
                if (saveTransaction.Contains(obj)) return;

                saveTransaction.Add(obj);

                if (!this.memoryBuffer.Contains(obj))
                {

                    var type = obj.GetType();
                    var keyProperty = type.GetProperty("Id");
                    if (keyProperty != null)
                    {
                        keyProperty.SetValue(obj, this.memoryBuffer.Count + 1, null);
                    }

                    this.memoryBuffer.Add(obj);
                }

                SaveProperties(obj.GetType(), obj);
            }
            finally
            {
                if (saveTransactionBegins) saveTransaction = null;
            }
        }

        /// <summary>
        /// Saves the specified entity collection.
        /// </summary>
        /// <param name="obj">The entity collection.</param>
        public void Save(IEnumerable<TEntity> obj)
        {
            foreach (var entity in obj)
            {
                this.Save(entity);
            }
        }

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        /// <param name="obj">The entity.</param>
        public void Remove(TEntity obj)
        {
            if (this.memoryBuffer.Contains(obj))
            {
                this.memoryBuffer.Remove(obj);
            }
        }
    
        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>.
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<TEntity> GetEnumerator()
        {
            return this.memoryBuffer.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.memoryBuffer.GetEnumerator();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Clear memory buffer.
        /// </summary>
        public void Clear()
        {
            this.memoryBuffer.Clear();
        }

        /// <summary>
        /// Saves properties
        /// </summary>
        /// <param name="type">Object types</param>
        /// <param name="obj">Object instance</param>
        private void SaveProperties(Type type, object obj)
        {
            foreach (var propertyInfo in type.GetProperties())
            {
                if (!propertyInfo.GetIndexParameters().Any())
                {
                    var propertyValue = propertyInfo.GetValue(obj, null);
                    this.SaveProperty(propertyInfo.PropertyType, propertyValue);
                }
            }
        }

        /// <summary>
        /// Saves property value in repositary structure
        /// </summary>
        /// <param name="propertyType">
        /// The property type.
        /// </param>
        /// <param name="propertyValue">
        /// The property value.
        /// </param>
        private void SaveProperty(Type propertyType, object propertyValue)
        {
            // one association
            if (propertyValue != null && propertyType.GetProperty("Id") != null)
            {
                var repositaryType = typeof(IRepositary<>).MakeGenericType(propertyType);
                var repositaryMethod = repositaryType.GetMethod("Save");
                var repositary = ServiceLocator.Current.GetInstance(repositaryType);
                if (repositary != null)
                {
                    repositaryMethod.Invoke(repositary, new[] { propertyValue });
                }
            }
            // many assoociation
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
            {
                var childCollection = (IEnumerable)propertyValue;
                if (childCollection != null)
                {
                    foreach (var subentity in childCollection)
                    {
                        SaveProperty(propertyType.GetGenericArguments().First(), subentity);
                    }
                }
            }
        }
    }
}
