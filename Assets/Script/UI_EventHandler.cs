using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/*
 * UI에 대해 클릭이든 드래그든 EventSystem이 이벤트를 탐지하여 이벤트를 전달할 때
 * 이를 캐치하고 콜백으로 작업을 수행
 * 
 * 이 이벤트 핸들러를 특정 UI 오브젝트에 적용을 하면,
 * 자식 UI 객체 중 하나에 UI 작업을 해도 이벤트 핸들러가 발동한다. 
 * 자식 객체에만 핸들러를 부착하면, 그것에 대해서만 발동한다.
 */
public class UI_EventHandler : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    // 이벤트 전파를 하기 위핸 멤버
    public Action<PointerEventData> OnClickHandler = null;
    public Action<PointerEventData> OnDragHandler = null;
    public Action<string> OnDropdownSelectHandler = null;  

    // 이벤트 시스템에서 전달하는 이벤트를 받기 위해선 특정 인터페이스 맞추어야 한다.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (OnClickHandler != null)
        {
            OnClickHandler.Invoke(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (OnDragHandler != null)
        {
            OnDragHandler.Invoke(eventData);
        }
    }
}
