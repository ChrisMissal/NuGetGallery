﻿using System.Runtime.Serialization;
using System.Web.Script.Serialization;

namespace NuGetGallery.FileAsyncUpload
{
    public class AsyncFileUploadProgressDetails
    {
        internal AsyncFileUploadProgressDetails(int contentLength)
        {
            ContentLength = contentLength;
        }

        [ScriptIgnore]
        public int ContentLength
        {
            get;
            set;
        }

        [ScriptIgnore]
        public int TotalBytesRead
        {
            get;
            set;
        }

        public int Progress
        {
            get
            {
                return (int)((long)TotalBytesRead * 100 / ContentLength);
            }
        }

        public string FileName
        {
            get;
            set;
        }

        [ScriptIgnore]
        public int BytesRemaining
        {
            get
            {
                return ContentLength - TotalBytesRead;
            }
        }
    }
}