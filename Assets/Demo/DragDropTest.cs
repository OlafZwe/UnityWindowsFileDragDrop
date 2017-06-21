using UnityEngine;

public class DragDropTest : MonoBehaviour {
    public TextMesh testNotification;
    public DragDropHandler dragDropHandler;

	// Use this for initialization
	void Start () {
        // register for file drag&drop event, and update text mesh with file paths
        dragDropHandler.fileDropEvent += delegate (string[] paths) {
            testNotification.text = "";
            for (int i = 0; i < paths.Length; ++i) {
                testNotification.text += paths[i] + "\n";
            }
        };
    }
}
