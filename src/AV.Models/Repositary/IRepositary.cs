// -----------------------------------------------------------------------
// <copyright file="IRepositary.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Models.Repositary
{
    using System;
    using System.Linq;

    /// <summary>
    ///   Base repositary interface
    /// </summary>
    /// <typeparam name="TEntity"> Entity to store </typeparam>
    public interface IRepositary<TEntity> : IQueryable<TEntity>, IDisposable
        where TEntity : class
    {
        #region Public Methods

        /// <summary>
        ///   Creates new instance of entity using repositary infrastructure
        /// </summary>
        /// <returns> New instance of entity type </returns>
        TEntity New();

        /// <summary>
        ///   Updates data using entity key
        /// </summary>
        /// <param name="obj"> Object to update </param>
        void Update(TEntity obj);

        /// <summary>
        ///   Removes the specified object.
        /// </summary>
        /// <param name="obj"> The object. </param>
        void Remove(TEntity obj);

        /// <summary>
        ///   Saves the specified object.
        /// </summary>
        /// <param name="obj"> The object. </param>
        void Save(TEntity obj);

        #endregion
    }
}