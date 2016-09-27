﻿namespace HttpClient.Caching.CacheStore
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using HttpClient.Caching.Attempt2.CacheStore;
    using Shouldly;
    using Xunit;

    public class ResponseSerializationTests
    {
        [Fact]
        public async Task IntegrationTest_Serialize()
        {
            var httpClient = new HttpClient();
            var httpResponseMessage = await httpClient.GetAsync("http://google.com");
            Console.WriteLine(httpResponseMessage.Headers.ToString());
            using (var fileStream = new FileStream("msg.bin", FileMode.Create))
            {
                await MessageContentSerializer.Serialize(httpResponseMessage, fileStream);
            }

            using (var fileStream = new FileStream("msg.bin", FileMode.Open))
            {
                httpResponseMessage = await MessageContentSerializer.DeserializeToResponse(fileStream);
            }
        }

        [Fact]
        public async Task IntegrationTest_Serialize_Deserialize()
        {
            var httpClient = new HttpClient();
            var httpResponseMessage = await httpClient.GetAsync("http://google.com");
            var contentLength = httpResponseMessage.Content.Headers.ContentLength;
                // access to make sure is populated http://aspnetwebstack.codeplex.com/discussions/388196
            var memoryStream = new MemoryStream();
            await MessageContentSerializer.Serialize(httpResponseMessage, memoryStream);
            memoryStream.Position = 0;
            var httpResponseMessage2 = await MessageContentSerializer.DeserializeToResponse(memoryStream);


            httpResponseMessage.StatusCode.ShouldBe(httpResponseMessage2.StatusCode);
            httpResponseMessage.ReasonPhrase.ShouldBe(httpResponseMessage2.ReasonPhrase);
            httpResponseMessage.Version.ShouldBe(httpResponseMessage2.Version);
            httpResponseMessage.Headers.ToString().ShouldBe(httpResponseMessage2.Headers.ToString());
            (await httpResponseMessage.Content.ReadAsStringAsync())
                .ShouldBe(await httpResponseMessage2.Content.ReadAsStringAsync());
            httpResponseMessage.Content.Headers.ToString().ShouldBe(httpResponseMessage2.Content.Headers.ToString());
        }
    }
}