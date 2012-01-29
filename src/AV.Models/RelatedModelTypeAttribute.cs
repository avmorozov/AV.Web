// -----------------------------------------------------------------------
// <copyright file="RelatedModelTypeAttribute.cs" company="ILabs NoWare">
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
    public sealed class RelatedModelTypeAttribute : Attribute
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref="RelatedModelTypeAttribute" /> class.
        /// </summary>
        /// <param name="relatedType"> The related type. </param>
        public RelatedModelTypeAttribute(Type relatedType, string relatedProperty)
        {
            this.RelatedType = relatedType;
            this.RelatedProperty = relatedProperty;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="RelatedModelTypeAttribute" /> class.
        /// </summary>
        /// <param name="relatedType"> The related type. </param>
        public RelatedModelTypeAttribute(Type relatedType) : this(relatedType, "Id") {}

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