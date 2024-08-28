using UnityEngine;
public class Util
{
    public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }

        return component;
    }

    // gameobject 자식 객체 찾기
    public static GameObject FindChild(GameObject go, string name = null, bool reculsive = false)
    {
        // 모든 게임 오브젝트는 Transform를 가짐, 이를 활용
        Transform transform = FindChild<Transform>(go, name, reculsive);
        if (transform == null)
        {
            return null;
        }

        return transform.gameObject;
    }

    // Fidn child object
    public static T FindChild<T>(GameObject go, string name = null, bool reculsive = false) where T : UnityEngine.Object
    {
        if (go == null)
        {
            return null;
        }

        if (reculsive)
        {
            // 컴포넌트 타입이 T인 자식 순회 
            foreach (T component in go.GetComponentsInChildren<T>())
            {
                // 이름을 전달하지 않으면 T 타입 컴포넌트를 갖는 자식 하나를 반환
                if (string.IsNullOrEmpty(name) || component.name == name)
                {
                    return component;
                }
            }
        }
        else
        {
            // (GameObject의 transform 컴포넌트를 통해 자식에 관한 정보에 접근)
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform transform = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || transform.name == name)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                    {
                        return component;
                    }
                }
            }
        }

        return null;
    }
}
