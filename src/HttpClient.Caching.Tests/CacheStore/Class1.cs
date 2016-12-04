namespace HttpClient.Caching.CacheStore
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class WriteQueue
    {
        private readonly string _cachePath;

        public WriteQueue(string cachePath = "cache")
        {
            _cachePath = cachePath;
        }

        public Stream GetFancyStream(Stream inner)
        {
            return new FancyStream(inner, _cachePath);
        }

        private class FancyStream : Stream
        {
            private readonly string _fileName;
            private readonly FileStream _fileStream;
            private readonly Stream _inner;
            private readonly string _tempFileName;

            public FancyStream(Stream inner, string path)
            {
                _inner = inner;
                if(!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                _fileName = Path.Combine(path, Guid.NewGuid().ToString());
                _tempFileName = $"{_fileName}.tmp";
                _fileStream = File.Open(_tempFileName, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }

            public override bool CanRead => _inner.CanRead;

            public override bool CanSeek => _inner.CanSeek;

            public override bool CanWrite => _inner.CanWrite;

            public override long Length => _inner.Length;

            public override long Position
            {
                get { return _inner.Position; }
                set { _inner.Position = value; }
            }

            public override void Flush()
            {
                _inner.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _inner.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _inner.SetLength(value);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _inner.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _inner.Write(buffer, offset, count);
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
                CancellationToken cancellationToken)
            {
                var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
                await _fileStream.WriteAsync(buffer, offset, read, cancellationToken);
                return read;
            }

            public override void Close()
            {
                _inner.Close();
                _fileStream.Close();
                File.Move(_tempFileName, _fileName);
            }
        }
    }

    public class WriteQueueTests
    {
        [Fact]
        public async Task Blah()
        {
            var writeQueue = new WriteQueue();

            var source = new byte[1024 * 100];
            for(var i = 0; i < source.Length; i++)
                source[i] = (byte) i;
            var memoryStream = new MemoryStream(source);

            var stream = writeQueue.GetFancyStream(memoryStream);

            var buffer = new byte[1024 * 16];

            while(await stream.ReadAsync(buffer, 0, buffer.Length) > 0)
            {}

            stream.Close();
        }
    }
}