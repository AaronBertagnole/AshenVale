using UnityEngine;

namespace AshenVale.CharacterRigs
{
    public sealed class RigPreviewControls : MonoBehaviour
    {
        public CutoutCharacterRig[] rigs;
        bool walk, masters;
        public Texture2D rangerMaster, warriorMaster;
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 350, 115), GUI.skin.box);
            GUILayout.Label("ASHEN VALE — CHARACTER RIG PROTOTYPE");
            walk = GUILayout.Toggle(walk, "Walk cycle (off = idle)");
            masters = GUILayout.Toggle(masters, "Show approved design references");
            if (GUILayout.Button("Face the other direction")) foreach (var rig in rigs) if (rig) rig.facingRight = !rig.facingRight;
            GUILayout.EndArea();
            foreach (var rig in rigs) if (rig) rig.walking = walk;
            if (masters)
            {
                float h = Mathf.Min(Screen.height - 155, 650), w = h * 2 / 3;
                if (rangerMaster) GUI.DrawTexture(new Rect(22, 140, w, h), rangerMaster, ScaleMode.ScaleToFit, true);
                if (warriorMaster) GUI.DrawTexture(new Rect(Screen.width - w - 22, 140, w, h), warriorMaster, ScaleMode.ScaleToFit, true);
            }
        }
    }
}
