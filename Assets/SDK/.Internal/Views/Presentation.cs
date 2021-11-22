using System;
using System.Collections.Generic;

namespace AfterNow.AnPrez.SDK.Internal.Views
{
    [Serializable]
    public class Presentation
    {
        public string id;

        public string presentation_name;

        public List<Location> locations = new List<Location>();

        public bool locked;
        public bool onlineOnly;
        public bool presenterLed;
        public bool offlineReady; //indicates whether a presentation is ready to be saved as an offline presentation. not used in server
        public bool hasUpdatedOffline; //indicates whether the presentation has updated in offline mode
        public PrezMatch match;

        public Presentation(string name, List<Location> locations)
        {
            this.presentation_name = name;

            this.locations.Clear();

            if (locations != null)
            {
                foreach (Location location in locations)
                {
                    this.locations.Add(location);
                }
            }
        }
    }

    [Serializable]
    public class PrezMatch
    {
        public string shortId;
        public string longId;
        public string presentationId;
    }

    [Serializable]
    public class APIPresentationResponse
    {
        public Presentation[] presentation;
    }
}
