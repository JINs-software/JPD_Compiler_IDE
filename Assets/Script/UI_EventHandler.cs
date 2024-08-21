using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/*
 * UI�� ���� Ŭ���̵� �巡�׵� EventSystem�� �̺�Ʈ�� Ž���Ͽ� �̺�Ʈ�� ������ ��
 * �̸� ĳġ�ϰ� �ݹ����� �۾��� ����
 * 
 * �� �̺�Ʈ �ڵ鷯�� Ư�� UI ������Ʈ�� ������ �ϸ�,
 * �ڽ� UI ��ü �� �ϳ��� UI �۾��� �ص� �̺�Ʈ �ڵ鷯�� �ߵ��Ѵ�. 
 * �ڽ� ��ü���� �ڵ鷯�� �����ϸ�, �װͿ� ���ؼ��� �ߵ��Ѵ�.
 */
public class UI_EventHandler : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    // �̺�Ʈ ���ĸ� �ϱ� ���� ���
    public Action<PointerEventData> OnClickHandler = null;
    public Action<PointerEventData> OnDragHandler = null;
    public Action<string> OnDropdownSelectHandler = null;  

    // �̺�Ʈ �ý��ۿ��� �����ϴ� �̺�Ʈ�� �ޱ� ���ؼ� Ư�� �������̽� ���߾�� �Ѵ�.
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
