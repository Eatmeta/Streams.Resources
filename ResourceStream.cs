using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streams.Resources
{
    public class ResourceReaderStream : Stream
    {
        public ResourceReaderStream(Stream stream, string key)
        {
            // You should not use stream in the constructor of wrapping stream.
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // if key not found yet: SeekValue();
            // if value is not read yet: ReadFieldValue(...)
            // else return 0;
        }

        private void SeekValue()
        {
            // while not end of stream read next section key, compare with required key and skip value if read key is not equal to required key
        }

        public override void Flush()
        {
            // nothing to do
        }
    }
}
