using UnityEngine;

namespace Cytus2.Views
{
    public class HoldNoteViewProvider : SingletonMonoBehavior<HoldNoteViewProvider>
    {
        public GameObject LinePrefab;
        public GameObject CompletedLinePrefab;
        public GameObject ProgressRingPrefab;
        public GameObject TrianglePrefab;
    }
}