// -----------------------------------------------------------------------
// <copyright file="IRepositary.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Models.Repositary
{
    using System.Linq;

    /// <summary>
    ///   Base repositary interface
    /// </summary>
    /// <typeparam name="TEntity"> Entity to store </typeparam>
    public interface IRepositary<TEntity> : IQueryable<TEntity>
        where TEntity : class
    {
        #region Public Methods

        /// <summary>
        ///   Creates new instance of entity using repositary infrastructure
        /// </summary>
        /// <returns> New instance of entity type </returns>
        TEntity New();

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