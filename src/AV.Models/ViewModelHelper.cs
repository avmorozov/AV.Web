// -----------------------------------------------------------------------
// <copyright file="ViewModelHelper.cs" company="ILabs NoWare">
//   (c) Alexander Morozov, 2012
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Practices.ServiceLocation;

namespace AV.Models
{
    /// <summary>
    ///   Common viewmodel functions.
    /// </summary>
    public static class ViewModelHelper
    {
        #region Constants and Fields

        /// <summary>
        ///   First or default Queryable generic method
        /// </summary>
        private static readonly MethodInfo FirstOrDefaultGenericMethod =
            typeof (Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(
                x => x.Name == "FirstOrDefault" && x.GetParameters().Length == 2);

        #endregion

        #region Public Methods

        /// <summary>
        ///   Fills view model properties using values from entity
        /// </summary>
        /// <param name="viewModel"> The view model </param>
        /// <param name="entity"> The entity </param>
        public static void Fill(this IViewModel viewModel, IEntity entity)
        {
            Dictionary<string, PropertyInfo> entityProps = entity.GetType().GetProperties()
                .ToDictionary(x => x.Name);

            IEnumerable<PropertyInfo> viewModelProps = viewModel.GetType().GetProperties()
                .Where(x => !x.GetIndexParameters().Any());

            foreach (PropertyInfo viewModelProperty in viewModelProps)
            {
                if (!entityProps.ContainsKey(viewModelProperty.Name))
                    continue;

                PropertyInfo entityProperty = entityProps[viewModelProperty.Name];
                object value = entityProperty.GetValue(entity, null);

                var relatedModelTypeAttribute =
                    viewModelProperty.GetCustomAttributes(typeof (RelatedModelTypeAttribute), true).FirstOrDefault() as
                    RelatedModelTypeAttribute;

                if (relatedModelTypeAttribute != null)
                {
                    Type relatedType = relatedModelTypeAttribute.RelatedType;
                    string relatedProperty = string.IsNullOrEmpty(relatedModelTypeAttribute.RelatedProperty)
                                                 ? "Id"
                                                 : relatedModelTypeAttribute.RelatedProperty;

                    value = relatedType.GetProperty(relatedProperty).GetValue(value, null);
                }
                viewModelProperty.SetValue(viewModel, value, null);
            }
        }

        /// <summary>
        ///   Updates entity properties by view model properies
        /// </summary>
        /// <param name="entity"> The entity. </param>
        public static void UpdateProperties(this IViewModel viewModel, IEntity entity)
        {
            Dictionary<string, PropertyInfo> entityProps = entity.GetType().GetProperties().ToDictionary(x => x.Name);
            PropertyInfo[] viewModelProps = viewModel.GetType().GetProperties();

            foreach (PropertyInfo viewModelProperty in viewModelProps)
            {
                if (!entityProps.ContainsKey(viewModelProperty.Name))
                    continue;

                object value = viewModelProperty.GetValue(viewModel, null);
                PropertyInfo entityProperty = entityProps[viewModelProperty.Name];

                var relatedModelTypeAttribute =
                    viewModelProperty.GetCustomAttributes(typeof (RelatedModelTypeAttribute), true).FirstOrDefault() as
                    RelatedModelTypeAttribute;

                if (relatedModelTypeAttribute != null)
                {
                    object instance = LoadOrCreateFromRepositary(relatedModelTypeAttribute, value);
                    entityProperty.SetValue(entity, instance, null);
                }
                else if (viewModelProperty.PropertyType.IsAssignableFrom(entityProperty.PropertyType))
                    entityProperty.SetValue(entity, value, null);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///   Load or create related entity from repositary
        /// </summary>
        /// <param name="attribute"> The related model type attribute. </param>
        /// <param name="value"> The value of related property. </param>
        /// <returns> Instance of related entity </returns>
        private static object LoadOrCreateFromRepositary(RelatedModelTypeAttribute attribute, object value)
        {
            Type relatedType = attribute.RelatedType;
            string relatedProperty = string.IsNullOrEmpty(attribute.RelatedProperty) ? "Id" : attribute.RelatedProperty;

            Type repositaryType = typeof (IRepositary<>).MakeGenericType(new[] {relatedType});
            object repositary = ServiceLocator.Current.GetInstance(repositaryType);

            ParameterExpression lambdaParam = Expression.Parameter(relatedType, "x");
            LambdaExpression predicate =
                Expression.Lambda(
                    Expression.Equal(Expression.Property(lambdaParam, relatedProperty), Expression.Constant(value)),
                    lambdaParam);

            MethodInfo firstOrDefaultMethod = FirstOrDefaultGenericMethod.MakeGenericMethod(new[] {relatedType});
            object instance = firstOrDefaultMethod.Invoke(null, new[] {repositary, predicate});

            if (instance == null)
            {
                instance = repositaryType.GetMethod("Create").Invoke(repositary, null);
                relatedType.GetProperty(relatedProperty).SetValue(instance, value, null);
            }

            return instance;
        }

        #endregion
    }
}