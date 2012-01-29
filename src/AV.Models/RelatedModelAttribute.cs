// -----------------------------------------------------------------------
// <copyright file="RelatedModelAttribute.cs" company="ILabs NoWare">
//   (c) Alexander Morozov, 2012
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace AV.Models
{
    /// <summary>
    ///   Attribute to define a reference to a model dectionary entity
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class RelatedModelAttribute : Attribute
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref="RelatedModelAttribute" /> class.
        /// </summary>
        /// <param name="relatedType"> The related type. </param>
        /// <param name="relatedProperty"> The related property </param>
        public RelatedModelAttribute(Type relatedType, string relatedProperty)
        {
            RelatedType = relatedType;
            RelatedProperty = relatedProperty;
        }
        
        #endregion

        #region Public Properties
        
        /// <summary>
        ///   Gets or sets RelatedProperty.
        /// </summary>
        public string RelatedProperty { get; private set; }

        /// <summary>
        ///   Gets or sets RelatedType.
        /// </summary>
        public Type RelatedType { get; private set; }

        #endregion
    }
}