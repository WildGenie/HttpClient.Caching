namespace CacheCow.Client.FileCacheStore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using CacheCow.Common;
    using NUnit.Framework;

    [TestFixture]
    public class ConcurrencyTests
    {
        [SetUp]
        public void Setup()
        {
            ThreadPool.SetMinThreads(100, 1000);
            _rootPath = Path.Combine(Path.GetTempPath(), _random.Next().ToString());
            Directory.CreateDirectory(_rootPath);
        }

        [TearDown]
        public void TearDown()
        {
            var retry = 0;
            while(retry < 3)
                try
                {
                    Directory.Delete(_rootPath, true);
                    break;
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    retry++;
                    Thread.Sleep(500);
                }
        }

        private string _rootPath;
        private readonly Random _random = new Random();
        private const int ConcurrencyLevel = 100;
        private const int WaitTimeOut = 20000; // 20 seconds   
        private readonly RandomResponseBuilder _responseBuilder = new RandomResponseBuilder(ConcurrencyLevel);

        private HttpRequestMessage GetMessage(int number)
        {
            return new HttpRequestMessage(HttpMethod.Get,
                "http://carmanager.softxnet.co.uk/api/car/" + number);
        }

        [Test]
        public void Test()
        {
            var store = new FileStore(_rootPath);

            var tasks = new List<Task>();
            HttpResponseMessage responseMessage = null;
            for(var i = 0; i < ConcurrencyLevel; i++)
            {
                var message = GetMessage(i%ConcurrencyLevel);
                var cacheKey = new CacheKey(message.RequestUri.ToString(), new string[0]);


                tasks.Add(new Task(
                    () => store.AddOrUpdate(cacheKey, _responseBuilder.Send(message))));

                tasks.Add(new Task(
                    () => store.TryGetValue(cacheKey, out responseMessage)));

                tasks.Add(new Task(
                    () => store.TryRemove(cacheKey)));
            }

            var randomisedList = new List<Task>();
            //while (tasks.Count>0)
            //{
            //    var i = _random.Next(tasks.Count);
            //    randomisedList.Add(tasks[i]);
            //    tasks.RemoveAt(i);
            //}

            //tasks = randomisedList;

            foreach(var task in tasks)
            {
                task.ContinueWith(t =>
                {
                    if(t.IsFaulted)
                        Assert.Fail(t.Exception.ToString());
                });
                task.Start();
            }

            var tt = DateTime.Now;
            var waited = Task.WaitAll(tasks.ToArray(), WaitTimeOut); //
            Console.WriteLine("Total milliseconds " + (DateTime.Now - tt).TotalMilliseconds);
            if(!waited)
                Assert.Fail("Timed out");
        }
    }
}