using System;
using System.IO;
using System.Text;

namespace Advent2025;

public class StringStream : StreamReader {
    public StringStream(string content)
        : base(new MemoryStream(Encoding.UTF8.GetBytes(content)))
    { }

    protected override void Dispose(bool disposing) {
        BaseStream.Dispose();
        base.Dispose(disposing);
    }
}
