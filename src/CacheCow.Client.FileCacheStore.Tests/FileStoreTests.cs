﻿namespace CacheCow.Client.FileCacheStore.Tests
{
    using System;
    using System.IO;
    using NUnit.Framework;
    using Simple.Data;

    [Ignore("Flickering tests. To Fix")]
    [TestFixture]
    public class FileStoreTests
    {
        [SetUp]
        public void Setup()
        {
            if(File.Exists(_dbFileName))
                File.Delete(_dbFileName);
            _store = new FileStore(Path.GetTempPath());
        }

        [TearDown]
        public void TearDown()
        {
            //if (File.Exists(_dbFileName))
            //    File.Delete(_dbFileName);
        }

        private readonly string _dbFileName = Path.Combine(Path.GetTempPath(), FileStore.CacheMetadataDbName);
        private FileStore _store;


        [Test]
        public void Clear_Test()
        {
            var database = Database.OpenFile(_dbFileName);
            var dateTime = DateTime.Now;
            database.Cache.Insert(new CacheItem
            {
                Domain = "d",
                Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                LastAccessed = dateTime,
                LastUpdated = dateTime,
                Size = 50
            });

            database.Cache.Insert(new CacheItem
            {
                Domain = "d",
                Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                LastAccessed = dateTime.AddDays(-1),
                LastUpdated = dateTime.AddDays(-1),
                Size = 100
            });

            _store.Clear();
            var list = _store.GetDomainSizes();
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void Get_Last_Item_ByDomain_Test()
        {
            var database = Database.OpenFile(_dbFileName);
            var dateTime = DateTime.Now;
            database.Cache.Insert(new CacheItem
            {
                Domain = "d",
                Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                LastAccessed = dateTime,
                LastUpdated = dateTime,
                Size = 100
            });

            database.Cache.Insert(new CacheItem
            {
                Domain = "d",
                Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                LastAccessed = dateTime,
                LastUpdated = dateTime,
                Size = 100
            });


            var cacheItemMetadata = _store.GetEarliestAccessedItem("d");
            Assert.AreEqual(100, cacheItemMetadata.Size);
            Assert.AreEqual("d", cacheItemMetadata.Domain);
            Assert.AreEqual(dateTime, cacheItemMetadata.LastAccessed);
        }

        [Test]
        public void Get_Last_Item_Test()
        {
            var database = Database.OpenFile(_dbFileName);
            var dateTime = DateTime.Now;
            database.Cache.Insert(new CacheItem
            {
                Domain = "c",
                Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                LastAccessed = dateTime,
                LastUpdated = dateTime,
                Size = 50
            });

            database.Cache.Insert(new CacheItem
            {
                Domain = "d",
                Hash = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                LastAccessed = dateTime.AddDays(-1),
                LastUpdated = dateTime.AddDays(-1),
                Size = 100
            });


            var cacheItemMetadata = _store.GetEarliestAccessedItem();
            Assert.AreEqual(100, cacheItemMetadata.Size);
            Assert.AreEqual("d", cacheItemMetadata.Domain);
            Assert.AreEqual(dateTime.AddDays(-1), cacheItemMetadata.LastAccessed);
        }

        [Test]
        public void Startup_No_File_Test()
        {
            Assert.IsTrue(File.Exists(_dbFileName));
        }
    }
}