using UnityEditor;

namespace UsefulTools
{
    public static class SelectionHelpers
    {

        [MenuItem("Selection/Select None %q")]
        public static void SelectNone()
        {
            Selection.objects = null;
        }
    }
}