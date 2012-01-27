// -----------------------------------------------------------------------
// <copyright file="IEntity.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Models
{
    /// <summary>
    ///   Interface that all entities must support
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        ///   Unified entity key
        /// </summary>
        long Id { get; set; }
    }
}