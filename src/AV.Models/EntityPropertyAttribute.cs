// -----------------------------------------------------------------------
// <copyright file="EntityPropertyAttribute.cs" company="ILabs NoWare">
//   (c) Alexander Morozov, 2012
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace AV.Models
{
    /// <summary>
    ///   Connects viewmodel property with entity property with another name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class EntityPropertyAttribute : Attribute
    {
        /// <summary>
        ///   Constructor of the attribute
        /// </summary>
        /// <param name="entityProperty"> </param>
        public EntityPropertyAttribute(string entityProperty)
        {
            EntityProperty = entityProperty;
        }

        /// <summary>
        ///   Entity propety name
        /// </summary>
        public string EntityProperty { get; private set; }
    }
}