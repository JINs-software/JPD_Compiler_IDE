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

    // gameobject �ڽ� ��ü ã��
    public static GameObject FindChild(GameObject go, string name = null, bool reculsive = false)
    {
        // ��� ���� ������Ʈ�� Transform�� ����, �̸� Ȱ��
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
            // ������Ʈ Ÿ���� T�� �ڽ� ��ȸ 
            foreach (T component in go.GetComponentsInChildren<T>())
            {
                // �̸��� �������� ������ T Ÿ�� ������Ʈ�� ���� �ڽ� �ϳ��� ��ȯ
                if (string.IsNullOrEmpty(name) || component.name == name)
                {
                    return component;
                }
            }
        }
        else
        {
            // (GameObject�� transform ������Ʈ�� ���� �ڽĿ� ���� ������ ����)
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
