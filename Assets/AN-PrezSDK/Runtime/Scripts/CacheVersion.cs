using System;

namespace AfterNow.PrezSDK
{
    [Serializable]
    internal class CacheVersion
    {
        internal DateTime lastModified;
        internal DateTime expiryTime;
        internal string presentationID;

        internal CacheVersion(DateTime lastModified, DateTime expiryTime, string prez_id)
        {
            this.lastModified = lastModified;
            this.expiryTime = expiryTime;
            presentationID = prez_id;
        }
    }
}