using System;

[Serializable]
public class CacheVersion
{
    public DateTime lastModified;
    public DateTime expiryTime;
    public string presentationID;

    public CacheVersion(DateTime lastModified,DateTime expiryTime,string prez_id)
    {
        this.lastModified = lastModified;
        this.expiryTime = expiryTime;
        presentationID = prez_id;
    }
}
