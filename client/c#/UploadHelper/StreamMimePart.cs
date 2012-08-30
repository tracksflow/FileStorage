using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UploadHelper
{
    public class StreamMimePart : MimePart
    {
        Stream _data;

        public void SetStream(Stream stream)
        {
            _data = stream;
        }

        public override Stream Data
        {
            get
            {
                return _data;
            }
        }
    }
}
