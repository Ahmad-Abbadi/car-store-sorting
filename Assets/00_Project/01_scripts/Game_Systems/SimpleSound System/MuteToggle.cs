using UnityEngine;
using UnityEngine.UI;

public class MuteToggle : MonoBehaviour
{
    [SerializeField] Image _volumeOn;
    [SerializeField] Image _volumeOff;
    [SerializeField] Toggle _toggle;


 

    public void Toggle(bool _isMuteOn)
    {

        if (!_isMuteOn) _toggle.targetGraphic = _volumeOn; 
        if (_isMuteOn) _toggle.targetGraphic = _volumeOff;

        SimpleAudioManager.instance.LockMute(_isMuteOn);
        SimpleAudioManager.instance.ToggleMute(_isMuteOn);


        _volumeOn.gameObject.SetActive(!_isMuteOn);
        _volumeOff.gameObject.SetActive(_isMuteOn);
        
    }
    




}
