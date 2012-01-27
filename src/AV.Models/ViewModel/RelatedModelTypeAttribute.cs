// -----------------------------------------------------------------------
// <copyright file="RelatedModelTypeAttribute.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Models.ViewModel
{
    using System;

    /// <summary>
    ///   Attribute to define a reference to a model dectionary entity
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RelatedModelTypeAttribute : Attribute
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref="RelatedModelTypeAttribute" /> class.
        /// </summary>
        /// <param name="relatedType"> The related type. </param>
        public RelatedModelTypeAttribute(Type relatedType)
        {
            RelatedType = relatedType;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets RelatedProperty.
        /// </summary>
        public string RelatedProperty { get; set; }

        /// <summary>
        ///   Gets or sets RelatedType.
        /// </summary>
        public Type RelatedType { get; set; }

        #endregion
    }
}