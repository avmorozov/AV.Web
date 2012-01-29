using System;

namespace AV.Models.ViewModel
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
        public RelatedModelTypeAttribute(Type relatedType, string relatedProperty = "Id")
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