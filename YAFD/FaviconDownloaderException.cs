using System;

namespace YetAnotherFaviconDownloader
{
    public enum FaviconDownloaderExceptionStatus
    {
        Error,
        NotFound
    }

    public class FaviconDownloaderException : Exception
    {
        private FaviconDownloaderExceptionStatus _status;
        public FaviconDownloaderExceptionStatus Status { get { return _status; } private set { _status = value; } }

        public FaviconDownloaderException() : this(FaviconDownloaderExceptionStatus.Error)
        {

        }

        public FaviconDownloaderException(FaviconDownloaderExceptionStatus status)
        {
            Status = status;
        }
    }
}
