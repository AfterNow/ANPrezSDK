using System;
using System.Collections.Generic;

    [Serializable]
    public class PresentationFilter
    {
        public Where where;

        public string[] include;

        public PresentationFilter(int presentationID, string[] include)
        {
            this.include = include;
            where = new Where(presentationID);
        }
    }

    [Serializable]
    public class Where
    {
        public int id;
        public Where(int id)
        {
            this.id = id;
        }
    }

    [Serializable]
    public class LoginResponse
    {
        public string id;
        public int userId;
    }

    [Serializable]
    public class PresentationResponse
    {
        public string name;
        public string created;
        public int id;
        public bool locked;
        public bool onlineOnly;
        public bool presenterLed;
        public PrezMatch match;
    }

    [Serializable]
    public class PresentationDetailsResponse
    {
        public int id;
        public string name;
        public string created;
    }

    [Serializable]
    public class LoginPost
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class MatchIdResponse
    {
        public string shortId;
        public string longId;
        public string presentationId;
        public string presenterId;
    }

    [Serializable]
    public class MatchPostBody
    {
        public string longId;
    }

    [Serializable]
    public class MatchFilter
    {
        public MatchWhere where = new MatchWhere();
    }

    [Serializable]
    public class MatchWhere
    {
        public string shortId;
    }

    [Serializable]
    public class PresentationList
    {
        public List<Presentation> presentations;
    }
