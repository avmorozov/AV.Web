// -----------------------------------------------------------------------
// <copyright file="ViewModelHelpersTest.cs" company="ILabs NoWare">
//   (c) Alexander Morozov, 2012
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using AV.Models;
using FluentAssertions;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AV.Web.Tests.Models.ViewModel
{
    /// <summary>
    ///   Tests viewmodel helper functions
    /// </summary>
    [TestClass]
    public class ViewModelHelpersTest
    {
        /// <summary>
        ///   Test view model properties filling from values in entity
        /// </summary>
        [TestMethod]
        public void SimplePropertyMappingFill()
        {
            var entity = new SimpleEntity
                             {
                                 Name = "Awesome name",
                                 Birthday = new DateTime(2000, 01, 02),
                                 Marrige = new DateTime(2010, 02, 03)
                             };
            var viewmodel = new SimpleViewModel();

            viewmodel.Fill(entity);

            viewmodel.Comment.Should().BeNullOrEmpty();
            viewmodel.Wedding.Should().Be(new DateTime(2010, 02, 03));
            viewmodel.Name.Should().Be("Awesome name");
            viewmodel.Birthday.Should().Be(new DateTime(2000, 01, 02));
        }

        /// <summary>
        ///   Test entity properties updating from values in view model
        /// </summary>
        [TestMethod]
        public void SimplePropertyMappingUpdate()
        {
            var entity = new SimpleEntity {Id = 1, Name = "Awesome name", Birthday = new DateTime(2000, 01, 02)};
            var viewmodel = new SimpleViewModel {Name = "Awefull name", Birthday = new DateTime(2001, 02, 03), Wedding = new DateTime(2010, 03, 04)};

            viewmodel.Update(entity);

            entity.Id.Should().Be(1);
            entity.Name.Should().Be("Awefull name");
            entity.Birthday.Should().Be(new DateTime(2001, 02, 03));
            entity.Marrige.Should().Be(new DateTime(2010, 03, 04));
        }

        /// <summary>
        ///   Test ViewModel filling from two entities
        /// </summary>
        [TestMethod]
        public void DictionaryPropertyFill()
        {
            var entity = new AggregateEntity
                             {
                                 Name = "Awesome Aggregate",
                                 EntityFromDictionary = new SimpleEntity {Name = "Awefull object"}
                             };
            var viewModel = new AggregateViewModel();

            viewModel.Fill(entity);

            viewModel.Name.Should().Be("Awesome Aggregate");
            viewModel.ValueFromDictionary.Should().Be("Awefull object");
        }

        /// <summary>
        ///   Test ViewModel updates entity
        /// </summary>
        [TestMethod]
        public void DictionaryPropertyUpdate()
        {
            var viewModel = new AggregateViewModel {Name = "Awesome name", ValueFromDictionary = "Awefull object"};
            var entity = new AggregateEntity();

            viewModel.Update(entity);

            entity.Name.Should().Be("Awesome name");
            entity.EntityFromDictionary.Should().BeNull();
        }
    }

    #region Sample view models & entities

    /// <summary>
    ///   Simple View Model
    /// </summary>
    public class SimpleViewModel : IViewModel
    {
        public string Name { get; set; }

        public string Comment { get; set; }

        public DateTime Birthday { get; set; }

        [EntityProperty("Marrige")]
        public DateTime Wedding { get; set; }        
    }

    /// <summary>
    ///   Simple entity
    /// </summary>
    public class SimpleEntity : IEntity
    {
        public string Name { get; set; }

        public DateTime Birthday { get; set; }

        public DateTime Marrige { get; set; }

        #region IEntity Members

        public long Id { get; set; }

        #endregion
    }

    /// <summary>
    ///   Aggregator sample entity
    /// </summary>
    public class AggregateEntity : IEntity
    {
        public string Name { get; set; }
        
        public SimpleEntity EntityFromDictionary { get; set; }

        #region IEntity Members

        public long Id { get; set; }

        #endregion
    }

    /// <summary>
    ///   Aggregator sampleViewModel
    /// </summary>
    public class AggregateViewModel : IViewModel
    {
        public string Name { get; set; }

        [EntityProperty("EntityFromDictionary")]
        [RelatedModel(typeof (SimpleEntity), "Name")]
        public string ValueFromDictionary { get; set; }
    }

    #endregion
}