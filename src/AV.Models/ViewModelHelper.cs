// -----------------------------------------------------------------------
// <copyright file="ViewModelHelper.cs" company="ILabs NoWare">
//   (c) Alexander Morozov, 2012
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AV.Models
{
    /// <summary>
    ///   Common viewmodel functions.
    /// </summary>
    public static class ViewModelHelper
    {
        #region Constants and Fields

        /// <summary>
        ///   Flags to select persisted properties
        /// </summary>
        private const BindingFlags PersistedPropertiesFlags =
            BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance;

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
            var viewModelType = viewModel.GetType();
            var entityType = entity.GetType();

            // select mapped properties
            var mapping = (from property in viewModelType.GetProperties(PersistedPropertiesFlags)
                           where !property.GetIndexParameters().Any() &&
                                 (entityType.GetProperty(property.Name, property.PropertyType, new Type[] {}) != null
                                  || property.GetCustomAttributes(typeof (EntityPropertyAttribute), true).Any()
                                  || property.GetCustomAttributes(typeof (RelatedModelAttribute), true).Any())
                           select GetPropertyMap(property, entity)).ToList();

            foreach (var propertyMap in mapping)
            {
                if (propertyMap.Value != null)
                    propertyMap.Key.SetValue(viewModel, propertyMap.Value, null);
            }
        }

        /// <summary>
        ///   Get property map
        /// </summary>
        /// <param name="property"> ViewModel propety </param>
        /// <param name="entity"> Entity property </param>
        /// <returns> </returns>
        private static KeyValuePair<PropertyInfo, object> GetPropertyMap(PropertyInfo property, IEntity entity)
        {
            var entityPropertyValue = GetEntityProperty(property, entity).GetValue(entity, null);

            var modelAttribute = property.GetCustomAttributes(typeof (RelatedModelAttribute), true)
                                     .FirstOrDefault() as RelatedModelAttribute;
            if (modelAttribute == null)
                return new KeyValuePair<PropertyInfo, object>(property, entityPropertyValue);

            var relatedValue = modelAttribute.RelatedType.GetProperty(modelAttribute.RelatedProperty)
                .GetValue(entityPropertyValue, null);
            return new KeyValuePair<PropertyInfo, object>(property, relatedValue);
        }

        /// <summary>
        ///   Updates entity properties by view model properies
        /// </summary>
        /// <param name="entity"> The entity. </param>
        public static void Update(this IViewModel viewModel, IEntity entity)
        {
            var viewModelType = viewModel.GetType();
            var entityType = entity.GetType();

            // select mapped properties
            var mapping = (from property in viewModelType.GetProperties(PersistedPropertiesFlags)
                           where !property.GetIndexParameters().Any() &&
                                 !property.GetCustomAttributes(typeof (RelatedModelAttribute), true).Any() &&
                                 (entityType.GetProperty(property.Name, property.PropertyType, new Type[] {}) != null
                                  || property.GetCustomAttributes(typeof (EntityPropertyAttribute), true).Any())
                           select new KeyValuePair<PropertyInfo, object>(
                               GetEntityProperty(property, entity),
                               property.GetValue(viewModel, null)))
                .ToList();

            foreach (var propertyMap in mapping)
            {
                if (propertyMap.Value != null)
                    propertyMap.Key.SetValue(entity, propertyMap.Value, null);
            }
        }

        private static PropertyInfo GetEntityProperty(PropertyInfo property, IEntity entity)
        {
            var entityType = entity.GetType();

            var propertyAttribute = property.GetCustomAttributes(typeof (EntityPropertyAttribute), true)
                                        .FirstOrDefault() as EntityPropertyAttribute;
            var entityPropertyName = (propertyAttribute != null)
                                         ? propertyAttribute.EntityProperty
                                         : property.Name;

            return entityType.GetProperty(entityPropertyName);
        }

        #endregion
    }
}