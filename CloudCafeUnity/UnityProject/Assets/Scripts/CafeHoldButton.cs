using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CloudCafe {
 public sealed class CafeHoldButton:MonoBehaviour,IPointerDownHandler,IPointerUpHandler {
  public Action<bool> Changed;
  public void OnPointerDown(PointerEventData data){if(data.button==PointerEventData.InputButton.Left&&GetComponent<Button>().interactable)Changed?.Invoke(true);}
  public void OnPointerUp(PointerEventData data){Changed?.Invoke(false);}
  void OnDisable(){Changed?.Invoke(false);}
 }
}
