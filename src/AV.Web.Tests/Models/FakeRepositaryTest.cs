// -----------------------------------------------------------------------
// <copyright file="FakeRepositaryTest.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Web.Tests.Models
{
    using System.Collections.Generic;
    using System.Linq;

    using AV.Models;

    using FluentAssertions;

    using Microsoft.Practices.ServiceLocation;
    using Microsoft.Practices.Unity;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///   Test for fake repositary realization
    /// </summary>
    [TestClass]
    public class FakeRepositaryTest
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref="FakeRepositaryTest" /> class.
        /// </summary>
        public FakeRepositaryTest()
        {
            IUnityContainer container = new UnityContainer();
            container.RegisterType<IRepositary<SimpleEntity>, FakeRepositary<SimpleEntity>>(
                new PerThreadLifetimeManager());
            container.RegisterType<IRepositary<AggregationEntity>, FakeRepositary<AggregationEntity>>(
                new PerThreadLifetimeManager());

            var serviceProvider = new UnityServiceLocator(container);
            ServiceLocator.SetLocatorProvider(() => serviceProvider);
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets the repositary for simple entities.
        /// </summary>
        /// <value> The repositary. </value>
        public FakeRepositary<AggregationEntity> AggregationEntitiesRepositary
        {
            get
            {
                return
                    ServiceLocator.Current.GetInstance<IRepositary<AggregationEntity>>() as
                    FakeRepositary<AggregationEntity>;
            }
        }

        /// <summary>
        ///   Gets the repositary for simple entities.
        /// </summary>
        /// <value> The repositary. </value>
        public FakeRepositary<SimpleEntity> SimpleEntitiesRepositary
        {
            get
            {
                return ServiceLocator.Current.GetInstance<IRepositary<SimpleEntity>>() as FakeRepositary<SimpleEntity>;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Clears the memory buffer in repositary.
        /// </summary>
        [TestInitialize]
        public void ClearMemoryBuffer()
        {
            this.SimpleEntitiesRepositary.Clear();
            this.AggregationEntitiesRepositary.Clear();
        }

        /// <summary>
        ///   Clears the repositary.
        /// </summary>
        [TestMethod]
        public void ClearRepositary()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);

            this.SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            this.SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(entity);

            this.SimpleEntitiesRepositary.Clear();
            this.SimpleEntitiesRepositary.MemoryBuffer.Should().BeEmpty();
        }

        /// <summary>
        ///   Save two connected entities (testing cascade save)
        /// </summary>
        [TestMethod]
        public void ConnectedEntitiesSave()
        {
            var aggregationEntity = new AggregationEntity { Name = "Awesome aggregation" };
            aggregationEntity.OneToOne = new SimpleEntity { Name = "Awesome string" };

            this.AggregationEntitiesRepositary.Save(aggregationEntity);

            this.AggregationEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            this.AggregationEntitiesRepositary.MemoryBuffer.Should().Contain(aggregationEntity);
            this.SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            this.SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(aggregationEntity.OneToOne);
        }

        /// <summary>
        ///   Delete one of two connected entities should work
        /// </summary>
        [TestMethod]
        public void DeleteConnectedEntities()
        {
            var aggregationEntity = new AggregationEntity { Name = "Awesome aggregation" };
            aggregationEntity.OneToOne = new SimpleEntity { Name = "Awesome string" };

            this.AggregationEntitiesRepositary.Save(aggregationEntity);
            this.SimpleEntitiesRepositary.Remove(aggregationEntity.OneToOne);

            this.AggregationEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            this.AggregationEntitiesRepositary.MemoryBuffer.Should().Contain(aggregationEntity);
            this.SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(0);
        }

        /// <summary>
        ///   Deletes the entity.
        /// </summary>
        [TestMethod]
        public void DeleteEntity()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);

            this.SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            this.SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(entity);

            this.SimpleEntitiesRepositary.Remove(entity);
            this.SimpleEntitiesRepositary.MemoryBuffer.Should().BeEmpty();
        }

        /// <summary>
        ///   Gets the saved entity.
        /// </summary>
        [TestMethod]
        public void GetSavedEntity()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);

            this.SimpleEntitiesRepositary.Single(x => x.Id == 1).Should().Be(entity);
        }

        /// <summary>
        ///   LINQ query can be executed.
        /// </summary>
        [TestMethod]
        public void LINQCanBeExecuted()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);

            var result = (from e in this.SimpleEntitiesRepositary where e.Name.Contains("Awesome") select e).First();

            result.Should().Be(entity);
        }

        /// <summary>
        ///   Save many to many associations
        /// </summary>
        [TestMethod]
        public void ManyToManyAssociationSave()
        {
            var nodes = new List<AggregationEntity>();
            const int NodesCount = 10;
            for (int i = 0; i < NodesCount; i++)
                nodes.Add(new AggregationEntity { Name = string.Format("node {0}", i) });

            foreach (var from in nodes)
            {
                foreach (var to in nodes)
                {
                    from.ManyToManyTo.Add(to);
                    to.ManyToManyFrom.Add(from);
                }
            }

            nodes.ForEach(x => this.AggregationEntitiesRepositary.Save(x));

            this.AggregationEntitiesRepositary.MemoryBuffer.Should().HaveCount(NodesCount);
            this.AggregationEntitiesRepositary.MemoryBuffer.Should().Contain(nodes);
        }

        /// <summary>
        ///   Save one to many associations
        /// </summary>
        [TestMethod]
        public void OneTwoManyAssociationSave()
        {
            var parent = new AggregationEntity { Name = "Parent" };
            for (int i = 0; i < 10; i++)
            {
                var child = new AggregationEntity { Name = string.Format("child {0}", i), OneToMany = parent };
                parent.ManyToOne.Add(child);
            }

            this.AggregationEntitiesRepositary.Save(parent);

            this.AggregationEntitiesRepositary.MemoryBuffer.Should().HaveCount(11);
            this.AggregationEntitiesRepositary.MemoryBuffer.Should().Contain(parent);
            this.AggregationEntitiesRepositary.MemoryBuffer.Should().Contain(parent.ManyToOne);
        }

        /// <summary>
        ///   Saves the entity.
        /// </summary>
        [TestMethod]
        public void SaveEntity()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);

            this.SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(entity);
        }

        /// <summary>
        ///   Updates the enity.
        /// </summary>
        [TestMethod]
        public void UpdateEntity()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);

            this.SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            this.SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(entity);

            entity.Name = "На на на";
            this.SimpleEntitiesRepositary.Save(entity);

            this.SimpleEntitiesRepositary.MemoryBuffer.Should().HaveCount(1);
            this.SimpleEntitiesRepositary.MemoryBuffer.Should().Contain(entity);
        }

        /// <summary>
        ///   Check update operation is updating properties
        /// </summary>
        [TestMethod]
        public void ReloadEntity()
        {
            var entity = new SimpleEntity { Name = "Awesome string" };
            this.SimpleEntitiesRepositary.Save(entity);
            var entityToUpdate = new SimpleEntity { Id = entity.Id, Name = "Something else" };

            this.SimpleEntitiesRepositary.Update(entityToUpdate);

            entityToUpdate.Name.Should().Be("Awesome string");
        }

        #endregion

        #region Sample entities
        /// <summary>
        ///   Sample entity to test aggregations
        /// </summary>
        public class AggregationEntity : IEntity
        {
            #region Constructors and Destructors

            public AggregationEntity()
            {
                this.ManyToOne = new List<AggregationEntity>();
                this.ManyToManyFrom = new List<AggregationEntity>();
                this.ManyToManyTo = new List<AggregationEntity>();
            }

            #endregion

            #region Public Properties

            public long Id { get; set; }

            public ICollection<AggregationEntity> ManyToManyFrom { get; set; }

            public ICollection<AggregationEntity> ManyToManyTo { get; set; }

            public ICollection<AggregationEntity> ManyToOne { get; set; }

            public string Name { get; set; }

            public AggregationEntity OneToMany { get; set; }

            public SimpleEntity OneToOne { get; set; }

            #endregion
        }

        /// <summary>
        ///   Sample simple entity to test NHibernate repositary
        /// </summary>
        public class SimpleEntity : IEntity
        {
            #region Public Properties

            /// <summary>
            ///   Gets or sets the id.
            /// </summary>
            /// <value> The identifier. </value>
            public long Id { get; set; }

            /// <summary>
            ///   Gets or sets the name.
            /// </summary>
            /// <value> The simple name. </value>
            public string Name { get; set; }

            #endregion
        } 
        #endregion
    }
}