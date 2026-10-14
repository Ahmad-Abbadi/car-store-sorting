using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SFXMap",menuName = "ScriptableObject/SFXmap" )]
public class GeneralSFXMapper : ScriptableObject
{
    public sound[] Sounds;

    /// <summary> Sound registered under the ID; clipless when not found. </summary>
    public GameSound GetSoundByID(string ID)
    {
        for (int i = 0; i < Sounds.Length; i++)
        {
            if (Sounds[i].soundID == ID) return Sounds[i].gameSound;
        }
        return default;
    }

}




[Serializable]
public struct sound
{
    public string soundID;
    public GameSound gameSound;
}
