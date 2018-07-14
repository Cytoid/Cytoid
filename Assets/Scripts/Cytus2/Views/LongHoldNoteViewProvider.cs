using UnityEngine;

namespace Cytus2.Views
{
    public class LongHoldNoteViewProvider : SingletonMonoBehavior<LongHoldNoteViewProvider>
    {
        public GameObject LinePrefab;
        public GameObject CompletedLinePrefab;
        public GameObject ProgressRingPrefab;
        public GameObject TrianglePrefab;
    }
}