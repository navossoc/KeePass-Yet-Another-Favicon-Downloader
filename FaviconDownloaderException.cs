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
        public FaviconDownloaderExceptionStatus Status { get; private set; }

        public FaviconDownloaderException() : this(FaviconDownloaderExceptionStatus.Error)
        {

        }

        public FaviconDownloaderException(FaviconDownloaderExceptionStatus status)
        {
            Status = status;
        }
    }
}
