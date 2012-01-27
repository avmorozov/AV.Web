// -----------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Models.ViewModel
{
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using AV.Models.Repositary;

    using Microsoft.Practices.ServiceLocation;

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
            typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(
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
            var entityProps = entity.GetType().GetProperties()                
                .ToDictionary(x => x.Name);

            var viewModelProps = viewModel.GetType().GetProperties()
                .Where(x => !x.GetIndexParameters().Any());

            foreach (var viewModelProperty in viewModelProps)
            {
                if (!entityProps.ContainsKey(viewModelProperty.Name))
                    continue;

                var entityProperty = entityProps[viewModelProperty.Name];
                var value = entityProperty.GetValue(entity, null);

                var relatedModelTypeAttribute =
                    viewModelProperty.GetCustomAttributes(typeof(RelatedModelTypeAttribute), true).FirstOrDefault() as
                    RelatedModelTypeAttribute;

                if (relatedModelTypeAttribute != null)
                {
                    var relatedType = relatedModelTypeAttribute.RelatedType;
                    var relatedProperty = string.IsNullOrEmpty(relatedModelTypeAttribute.RelatedProperty)
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
            var entityProps = entity.GetType().GetProperties().ToDictionary(x => x.Name);
            var viewModelProps = viewModel.GetType().GetProperties();

            foreach (var viewModelProperty in viewModelProps)
            {
                if (!entityProps.ContainsKey(viewModelProperty.Name))
                    continue;

                var value = viewModelProperty.GetValue(viewModel, null);
                var entityProperty = entityProps[viewModelProperty.Name];

                var relatedModelTypeAttribute =
                    viewModelProperty.GetCustomAttributes(typeof(RelatedModelTypeAttribute), true).FirstOrDefault() as
                    RelatedModelTypeAttribute;

                if (relatedModelTypeAttribute != null)
                {
                    var instance = LoadOrCreateFromRepositary(relatedModelTypeAttribute, value);
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
            var relatedType = attribute.RelatedType;
            var relatedProperty = string.IsNullOrEmpty(attribute.RelatedProperty) ? "Id" : attribute.RelatedProperty;

            var repositaryType = typeof(IRepositary<>).MakeGenericType(new[] { relatedType });
            var repositary = ServiceLocator.Current.GetInstance(repositaryType);

            var lambdaParam = Expression.Parameter(relatedType, "x");
            var predicate =
                Expression.Lambda(
                    Expression.Equal(Expression.Property(lambdaParam, relatedProperty), Expression.Constant(value)),
                    lambdaParam);

            var firstOrDefaultMethod = FirstOrDefaultGenericMethod.MakeGenericMethod(new[] { relatedType });
            var instance = firstOrDefaultMethod.Invoke(null, new[] { repositary, predicate });

            if (instance == null)
            {
                instance = repositaryType.GetMethod("New").Invoke(repositary, null);
                relatedType.GetProperty(relatedProperty).SetValue(instance, value, null);
            }

            return instance;
        }

        #endregion
    }
}