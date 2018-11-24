using System;
using System.Collections.Generic;
using System.Text;
using System.Buffers;

namespace SocketCommunicationFoundation
{
    public static class KestrelMemoryPool
    {
        public static MemoryPool<byte> Create()
        {
#if DEBUG1
            return new DiagnosticMemoryPool(CreateSlabMemoryPool());
#else
            return CreateSlabMemoryPool();
#endif
        }

        public static MemoryPool<byte> CreateSlabMemoryPool()
        {
            return new SlabMemoryPool();
        }

        public static readonly int MinimumSegmentSize = 4096;
    }

    public class SlabMemoryPool : MemoryPool<byte>
    {
        public override int MaxBufferSize => 1024 * 512; // 512KB

        public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }
    }
}
