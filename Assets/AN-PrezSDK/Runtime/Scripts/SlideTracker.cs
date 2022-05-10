using System.Collections.Generic;

namespace AfterNow.PrezSDK
{
    internal class SlideTracker
    {
        //using linked list because we only care about last elements. arraylist is ineffective here
        private LinkedList<int> _linkedList = new LinkedList<int>();

        /// <summary>
        /// When we switch to a new slide, we add the previous slide in here
        /// </summary>
        /// <param name="index"></param>
        internal void AddLastSlide(int index)
        {
            if (_linkedList.Count > 0 && _linkedList.Last.Value == index) return;

            _linkedList.AddLast(index);
        }

        /// <summary>
        /// We use this to get the previous slide instead of currentSlide-1 logic
        /// </summary>
        /// <returns></returns>
        internal int GetPreviousSlide()
        {
            if (_linkedList.Count > 0)
            {
                int previousSlide = _linkedList.Last.Value;
                _linkedList.RemoveLast();
                return previousSlide;
            }
            else return 0;
        }

        /// <summary>
        /// Clear every history. When presentation ends.
        /// </summary>
        internal void Clear()
        {
            _linkedList.Clear();
        }
    }
}
