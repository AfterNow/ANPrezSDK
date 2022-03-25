using System;

namespace AfterNow.PrezSDK
{
    [Serializable]
    internal class CacheVersion
    {
        public DateTime lastModified;
        public DateTime expiryTime;
        public string presentationID;

        public CacheVersion(DateTime lastModified, DateTime expiryTime, string prez_id)
        {
            this.lastModified = lastModified;
            this.expiryTime = expiryTime;
            presentationID = prez_id;
        }
    }
}
