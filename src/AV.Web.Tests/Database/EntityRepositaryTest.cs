// -----------------------------------------------------------------------
// <copyright file="EntityRepositaryTest.cs" company="Александр Морозов">
//   (c) Александр Морозов, 2012
// </copyright>
// -----------------------------------------------------------------------

namespace AV.Web.Tests.Database
{
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.ModelConfiguration;
    using System.Linq;

    using AV.Database;
    using AV.Models;
    using AV.Web.Tests.Models;

    using FluentAssertions;

    using Microsoft.Practices.ServiceLocation;
    using Microsoft.Practices.Unity;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EntityRepositaryTest
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Init database file
        /// </summary>
        /// <param name="context"> </param>
        [ClassInitialize]
        public static void TestClassInitialize(TestContext context)
        {
            Database.DefaultConnectionFactory = new SqlCeConnectionFactory("System.Data.SqlServerCe.4.0");
            using (var connection = Database.DefaultConnectionFactory.CreateConnection("AV.Web.Tests.Database.SampleDb")
                )
            {
                if (Database.Exists(connection))
                {
                    Database.Delete(connection);
                }
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="FakeRepositaryTest" /> class.
        /// </summary>
        public EntityRepositaryTest()
        {
            IUnityContainer container = new UnityContainer();
            container.RegisterInstance(typeof(DbContext), new SampleDb());
            container.RegisterType<IRepositary<SimpleDbEntity>, EntityRepositary<SimpleDbEntity>>(
                new PerThreadLifetimeManager());
            container.RegisterType<IRepositary<AggregationDbEntity>, EntityRepositary<AggregationDbEntity>>(
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
        public EntityRepositary<AggregationDbEntity> AggregationEntitiesRepositary
        {
            get
            {
                return
                    ServiceLocator.Current.GetInstance<IRepositary<AggregationDbEntity>>() as
                    EntityRepositary<AggregationDbEntity>;
            }
        }

        /// <summary>
        ///   Gets the repositary for simple entities.
        /// </summary>
        /// <value> The repositary. </value>
        public EntityRepositary<SimpleDbEntity> SimpleEntitiesRepositary
        {
            get
            {
                return
                    ServiceLocator.Current.GetInstance<IRepositary<SimpleDbEntity>>() as
                    EntityRepositary<SimpleDbEntity>;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   Clears the memory buffer in repositary.
        /// </summary>
        [TestInitialize]
        public void ClearTables()
        {
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;

            dbContext.Database.ExecuteSqlCommand("DELETE FROM AggregationDbEntityAggregationDbEntities");
            dbContext.Database.ExecuteSqlCommand("UPDATE AggregationDbEntities SET OneToMany_Id = NULL");
            dbContext.Database.ExecuteSqlCommand("DELETE FROM SimpleDbEntities");
            dbContext.Database.ExecuteSqlCommand("DELETE FROM AggregationDbEntities");
            dbContext.SaveChanges();

            dbContext.AggregationEntities.ToList().ForEach(x => dbContext.AggregationEntities.Remove(x));
            dbContext.SaveChanges();
        }

        /// <summary>
        ///   Save two connected entities (testing cascade save)
        /// </summary>
        [TestMethod]
        public void ConnectedEntitiesSave()
        {
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            var aggregationEntity = new AggregationDbEntity { Name = "Awesome aggregation" };
            aggregationEntity.OneToOne = new SimpleDbEntity { Name = "Awesome string" };

            AggregationEntitiesRepositary.Save(aggregationEntity);

            dbContext.AggregationEntities.Should().HaveCount(1);
            dbContext.AggregationEntities.Should().Contain(x => x.Id == aggregationEntity.Id);
            dbContext.SimpleEntities.Should().HaveCount(1);
            dbContext.SimpleEntities.Should().Contain(x => x.Id == aggregationEntity.OneToOne.Id);
        }

        /// <summary>
        ///   Delete one of two connected entities should work
        /// </summary>
        [TestMethod]
        public void DeleteConnectedEntities()
        {
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            var aggregationEntity = new AggregationDbEntity { Name = "Awesome aggregation" };
            aggregationEntity.OneToOne = new SimpleDbEntity { Name = "Awesome string" };

            AggregationEntitiesRepositary.Save(aggregationEntity);
            SimpleEntitiesRepositary.Remove(aggregationEntity.OneToOne);

            dbContext.AggregationEntities.Should().HaveCount(1);
            dbContext.SimpleEntities.Should().HaveCount(0);
        }

        /// <summary>
        ///   Deletes the entity.
        /// </summary>
        [TestMethod]
        public void DeleteEntity()
        {
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            var entity = new SimpleDbEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);

            dbContext.SimpleEntities.Should().HaveCount(1);

            SimpleEntitiesRepositary.Remove(entity);
            dbContext.SimpleEntities.Should().HaveCount(0);
        }

        /// <summary>
        ///   Gets the saved entity.
        /// </summary>
        [TestMethod]
        public void GetSavedEntity()
        {
            var entity = new SimpleDbEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);

            SimpleEntitiesRepositary.Single(x => x.Id == entity.Id).Name.Should().Be("Awesome string");
        }

        /// <summary>
        ///   LINQ query can be executed.
        /// </summary>
        [TestMethod]
        public void LINQCanBeExecuted()
        {
            var entity = new SimpleDbEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);

            var result = (from e in SimpleEntitiesRepositary where e.Name.Contains("Awesome") select e).First();

            result.Name.Should().Be("Awesome string");
        }

        /// <summary>
        ///   Save many to many associations
        /// </summary>
        [TestMethod]
        public void ManyToManyAssociationSave()
        {
            var nodes = new List<AggregationDbEntity>();
            const int NodesCount = 10;
            for (int i = 0; i < NodesCount; i++)
            {
                nodes.Add(new AggregationDbEntity { Name = string.Format("node {0}", i) });
            }

            foreach (var from in nodes)
            {
                foreach (var to in nodes)
                {
                    from.ManyToManyTo.Add(to);
                    to.ManyToManyFrom.Add(from);
                }
            }

            nodes.ForEach(x => AggregationEntitiesRepositary.Save(x));

            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            dbContext.AggregationEntities.Should().HaveCount(NodesCount);
            dbContext.AggregationEntities.Should().Contain(nodes);
        }

        /// <summary>
        ///   Save one to many associations
        /// </summary>
        [TestMethod]
        public void OneTwoManyAssociationSave()
        {
            var parent = new AggregationDbEntity { Name = "Parent" };
            for (int i = 0; i < 10; i++)
            {
                var child = new AggregationDbEntity { Name = string.Format("child {0}", i), OneToMany = parent };
                parent.ManyToOne.Add(child);
            }

            AggregationEntitiesRepositary.Save(parent);

            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            dbContext.AggregationEntities.Should().HaveCount(11);
            dbContext.AggregationEntities.Should().Contain(parent);
            dbContext.AggregationEntities.Should().Contain(parent.ManyToOne);
        }

        /// <summary>
        ///   Saves the entity.
        /// </summary>
        [TestMethod]
        public void SaveEntity()
        {
            var entity = new SimpleDbEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);

            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            dbContext.SimpleEntities.Should().Contain(x => x.Id == entity.Id);
        }

        /// <summary>
        ///   Updates the enity.
        /// </summary>
        [TestMethod]
        public void UpdateEntity()
        {
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            var entity = new SimpleDbEntity { Name = "Awesome string" };
            SimpleEntitiesRepositary.Save(entity);
            dbContext.SimpleEntities.Should().HaveCount(1);
            dbContext.SimpleEntities.Should().Contain(x => x.Id == entity.Id);

            entity.Name = "На на на";
            SimpleEntitiesRepositary.Save(entity);

            dbContext.SimpleEntities.Should().HaveCount(1);
            var savedEntity = dbContext.SimpleEntities.Single(x => x.Id == entity.Id);
            savedEntity.Name.Should().Be("На на на");
        }

        /// <summary>
        ///   Check update operation is updating properties
        /// </summary>
        [TestMethod]
        public void ReloadEntity()
        {
            var dbContext = ServiceLocator.Current.GetInstance<DbContext>() as SampleDb;
            var entity = new SimpleDbEntity { Name =  "Awefull string"};
            SimpleEntitiesRepositary.Save(entity);            
            dbContext.Database.ExecuteSqlCommand("UPDATE SimpleDbEntities SET Name = {0} WHERE ID={1}", "Awesome string", entity.Id);
            SimpleEntitiesRepositary.Update(entity);
            entity.Name.Should().Be("Awesome string");
        }

        /// <summary>
        /// Check that entity with lazy loading is created
        /// </summary>
        [TestMethod]
        public void CreateEntity()
        {
            var entity = AggregationEntitiesRepositary.Create();
            entity.ManyToManyFrom.Should().NotBeNull();
            entity.ManyToManyTo.Should().NotBeNull();
            entity.ManyToOne.Should().NotBeNull();
        }

        #endregion
    }

    #region Sample Entities

    /// <summary>
    ///   Sample entity to test aggregations
    /// </summary>
    public class AggregationDbEntity : IEntity
    {
        #region Constructors and Destructors

        public AggregationDbEntity()
        {
            ManyToOne = new List<AggregationDbEntity>();
            ManyToManyFrom = new List<AggregationDbEntity>();
            ManyToManyTo = new List<AggregationDbEntity>();
        }

        #endregion

        #region Public Properties

        public long Id { get; set; }

        public virtual ICollection<AggregationDbEntity> ManyToManyFrom { get; set; }

        public virtual ICollection<AggregationDbEntity> ManyToManyTo { get; set; }

        public virtual ICollection<AggregationDbEntity> ManyToOne { get; set; }

        public string Name { get; set; }

        public virtual AggregationDbEntity OneToMany { get; set; }

        public virtual SimpleDbEntity OneToOne { get; set; }

        #endregion
    }

    /// <summary>
    ///   Sample simple entity to test NHibernate repositary
    /// </summary>
    public class SimpleDbEntity : IEntity
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

    public class SampleDb : DbContext
    {
        public DbSet<SimpleDbEntity> SimpleEntities { get; set; }

        public DbSet<AggregationDbEntity> AggregationEntities { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new SimpleEntityConfiguration());
            modelBuilder.Configurations.Add(new AggregationEntityConfiguration());
        }
    }

    public class SimpleEntityConfiguration : EntityTypeConfiguration<SimpleDbEntity>
    {
        public SimpleEntityConfiguration()
        {
            HasKey(x => x.Id);
            Property(x => x.Name).IsRequired();
        }
    }

    public class AggregationEntityConfiguration : EntityTypeConfiguration<AggregationDbEntity>
    {
        public AggregationEntityConfiguration()
        {
            HasKey(x => x.Id);
            Property(x => x.Name);

            HasOptional(x => x.OneToOne).WithOptionalPrincipal().WillCascadeOnDelete();
            HasOptional(x => x.OneToMany).WithMany(x => x.ManyToOne);
            HasMany(x => x.ManyToManyFrom).WithMany(x => x.ManyToManyTo);
        }
    }

    #endregion
}