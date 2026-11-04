using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MemoryWhitebox
{
    // Authored on B's UI keys; the panel's old onClick stays only for Stop/Eject.
    // No input state is stored in a second controller.
    public sealed class TapeTransportKey : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public TapeTransportPanel panel;
        public int command;
        public void OnPointerDown(PointerEventData data)
        {
            var button=GetComponent<Button>();
            if(data.button==PointerEventData.InputButton.Left && button!=null && button.IsInteractable())panel?.Hold(command);
        }
        public void OnPointerUp(PointerEventData data)
        {if(data.button==PointerEventData.InputButton.Left)Release();}
        public void OnPointerExit(PointerEventData data)=>Release();
        void OnDisable()=>Release();
        void Release(){if(panel!=null && panel.deck!=null)panel.deck.ReleaseTransportKey(command);}
    }
}
