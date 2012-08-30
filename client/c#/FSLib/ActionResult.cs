using System.IO;

namespace FSLib
{
    public enum ActionStatus
    {
        Ok,
        Error
    }

    public class SimpleActionResult
    {
        public ActionStatus Status;
        public string ErrorDescription;
        public SimpleActionResult() { }
        public SimpleActionResult(SimpleActionResult r)
        {
            Status = r.Status;
            ErrorDescription = r.ErrorDescription;
        }
    }

    public class LongActionResult : SimpleActionResult
    {
        public long Value;
        public LongActionResult() : base() { }
        public LongActionResult(SimpleActionResult r) : base(r) { }
    }

    public class StreamActionResult : SimpleActionResult
    {
        public Stream Value;
        public long Size;
        public StreamActionResult() : base() { }
        public StreamActionResult(SimpleActionResult r) : base(r) { }
    }

    public class StringActionResult : SimpleActionResult
    {
        public string Value;
        public StringActionResult() : base() { }
        public StringActionResult(SimpleActionResult r) : base(r) { }
    }
}
