using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    internal class TarIOFileItemStream : Stream
    {
        private long              position;
        private StreamAccessMode  mode;
        private TarIOBlockManager manager;
        private Stream            baseStream;
        private MemoryStream      suffixStream;
        private Pair<long, long>  fileEntry;


        public bool Closed { get; private set; }
        public long Offset { get { return fileEntry.Item1; } }
        public long PhysicalLength { get { return fileEntry.Item2; } }


        public override bool CanRead
        {
            get { return manager.baseStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return manager.baseStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return manager.baseStream.CanWrite; }
        }

        public override long Position
        {
            get { AssertRandomAccess(); return this.position; }
            set { AssertRandomAccess(); this.position = value; }
        }

        public override long Length
        {
            get
            {
                this.AssertNotDisposed();
                checked 
                { 
                    return PhysicalLength + (suffixStream == null ? 0 : suffixStream.Length); 
                }
            }
        }


        internal TarIOFileItemStream(long offset, long length, Stream baseStream)
        {
            this.baseStream = baseStream;
            this.mode       = StreamAccessMode.Sequential;
            this.fileEntry  = new Pair<long, long>(offset, length);
        }
        internal TarIOFileItemStream(long offset, long length, TarIOBlockManager manager)
        {
            this.manager      = manager;
            this.suffixStream = new MemoryStream();
            this.mode         = StreamAccessMode.Random;
            this.fileEntry    = new Pair<long, long>(offset, length);
        }

        private void AssertNotDisposed()
        {
            if (this.Closed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }
        private void AssertRandomAccess()
        {
            if (this.mode != StreamAccessMode.Random)
            {
                throw new NotSupportedException("The underlying stream does not support random access.");
            }
        }
               

        public override void Flush()
        {
            this.AssertNotDisposed();
            manager.SaveEntry(this);
        }

        public override void Close()
        {
            this.Close(true);
        }
        public void Close(bool flush)
        {
            this.Closed = true;

            if (flush)
            {
                this.suffixStream.Flush();
                this.Flush();
                this.suffixStream.Close();
            }
        }

        public void SaveToPhysical()
        {
            this.AssertNotDisposed();

            lock (manager)
            {
                manager.baseStream.Position = this.Offset + this.PhysicalLength;
                this.suffixStream.CopyTo(manager.baseStream);
                this.suffixStream.SetLength(0);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            this.AssertNotDisposed();

            if (buffer.Length < count)
            {
                throw new ArgumentException("Count greater than length of the supplied buffer");
            }

            int bytesRead = 0;

            // Read from the physical stream
            lock (manager)
            {
                if (this.Position < this.PhysicalLength)
                {
                    bytesRead = (int)Math.Min(count, this.PhysicalLength - this.Position);

                    if (CanSeek)
                    {
                        manager.baseStream.Position = this.Offset + this.Position;
                        manager.baseStream.Read(buffer, offset, bytesRead);
                    }
                    else
                    {
                        long delta = this.Offset - manager.Position;

                        if (delta < 0)
                        {
                            throw new InvalidOperationException("The current access mode only supports sequential reading");
                        }
                        else if (delta > Int32.MaxValue)
                        {
                            throw new InvalidOperationException("The current access mode only supports 32-bit offsets");
                        }
                        else
                        {
                            manager.baseStream.Read(new byte[delta], 0, (int)delta);
                        }
                        manager.baseStream.Read(buffer, offset, bytesRead);
                        manager.Position += bytesRead + delta;
                    }
                    this.Position += bytesRead;
                    offset += bytesRead;
                }
            }

            // If in the suffix stream
            if (bytesRead != count)
            {
                this.suffixStream.Position = this.Position - this.PhysicalLength;
                int read = this.suffixStream.Read(buffer, offset, count - bytesRead);

                this.Position += read;
                bytesRead += read;
            }
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            this.AssertNotDisposed();

            checked
            {
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        this.Position = offset;
                        break;
                    case SeekOrigin.Current:
                        this.Position += offset;
                        break;
                    case SeekOrigin.End:
                        this.Position = this.Length + offset;
                        break;
                }
            }
            if (this.Position > this.Length || this.Position < 0)
            {
                throw new IOException("Position advanced beyond the length of the stream");
            }
            return this.Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.AssertNotDisposed();

            if (buffer.Length < count)
            {
                throw new ArgumentException("Count greater than length of the supplied buffer");
            }

            // Write to the physical stream
            lock (manager)
            {
                if (this.Position < this.PhysicalLength)
                {
                    int localCount = (int)Math.Min(count, this.PhysicalLength - this.Position);

                    manager.baseStream.Position = this.Offset + this.Position;
                    manager.baseStream.Write(buffer, offset, localCount);

                    this.Position += localCount;
                    offset += localCount;
                    count -= localCount;
                }
            }

            // If in the suffix stream
            if (count != 0)
            {
                this.suffixStream.Position = this.Position - this.PhysicalLength;
                this.suffixStream.Write(buffer, offset, count);
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    manager.SaveEntry(this);
                    this.Closed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}