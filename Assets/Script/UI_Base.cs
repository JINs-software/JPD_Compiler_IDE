using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Base : MonoBehaviour
{
    Dictionary<Type, UnityEngine.Object[]> Objects = new Dictionary<Type, UnityEngine.Object[]>();

    // Reflection 활용
    // 이름을 보고 이름에 해당하는 오브젝트를 찾아 연동
    protected void Bind<T>(Type type) where T : UnityEngine.Object // T라는 컴포넌트를 찾아, 이에 해당하는 객체를 맵핑
    {
        // C# 만의 고유 기능인 Enum 이름 추출 활용
        string[] enumNames = Enum.GetNames(type);

        UnityEngine.Object[] objects = new UnityEngine.Object[enumNames.Length];
        Objects.Add(typeof(T), objects);

        // 일반 게임 오브젝트 추출
        for (int i = 0; i < enumNames.Length; i++)
        {
            if (typeof(T) == typeof(GameObject))
            {
                objects[i] = Utill.FindChild(gameObject, enumNames[i], true);
            }
            else
            {
                objects[i] = Utill.FindChild<T>(gameObject, enumNames[i], true);
            }

            if (objects[i] == null)
            {
                Debug.Log($"Failed to bind({enumNames[i]})");
            }
        }
    }

    protected Text GetText(int idx) { return Get<Text>(idx); }
    protected Button GetButton(int idx) { return Get<Button>(idx); }
    protected Image GetImage(int idx) { return Get<Image>(idx); }
    protected Dropdown GetDropdown(int idx) { return Get<Dropdown>(idx); }
    protected Toggle GetToggle(int idx) { return Get<Toggle>(idx); }
    protected InputField GetInputField(int idx) { return Get<InputField>(idx); }

    // usage: Get<Text>((int)Texts.ScoreText).text = ".....";
    protected T Get<T>(int index) where T : UnityEngine.Object
    {
        UnityEngine.Object[] objects = null;
        if (Objects.TryGetValue(typeof(T), out objects) == false)
        {
            return null;
        }

        return objects[index] as T; // UnityEngine 형에서 T 형으로 타입 캐스팅
    }

    public static void BindEvent(GameObject go, Action<PointerEventData> action, Define.UIEvent type = Define.UIEvent.Click)
    {
        UI_EventHandler evt = Utill.GetOrAddComponent<UI_EventHandler>(go);

        switch (type)
        {
            case Define.UIEvent.Click:
                evt.OnClickHandler -= action;
                evt.OnClickHandler += action;
                break;
            case Define.UIEvent.Drag:
                evt.OnDragHandler -= action;
                evt.OnDragHandler += action;
                break;
        }
    }
}
