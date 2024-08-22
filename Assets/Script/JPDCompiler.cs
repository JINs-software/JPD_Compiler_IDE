using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class JPDCompiler : MonoBehaviour
{
    static JPDCompiler s_Instance;
    public static JPDCompiler Instance {get { Init(); return s_Instance; }}

    private void Start()
    {
        Init();
    }
    private static void Init()
    {
        GameObject go = GameObject.Find("@JPDCompiler");
        if (go == null)
        {
            go = new GameObject { name = "@JPDCompiler" };
            go.AddComponent<JPDCompiler>();
        }

        DontDestroyOnLoad(go);
        s_Instance = go.GetComponent<JPDCompiler>();
    }

    private int m_CompileMode = 0;
    string m_ServerPath = string.Empty;
    string m_ClientPath = string.Empty;
    JPD m_JPD = new JPD();

    public JPD_ITEM AddJpdNamespace(string name)
    {
        foreach(var item in m_JPD.JpdItems)
        {
            if(item.Namespace == name)
            {
                return null;
            }
        }

        int namespaceID = m_JPD.JpdItems.Count;
        JPD_ITEM jpdItem = new JPD_ITEM(name, namespaceID.ToString());
        jpdItem.Namespace = name;
        jpdItem.ID = namespaceID.ToString();
        m_JPD.JpdItems.Add(jpdItem);

        return jpdItem;
    }

    public void SaveJsonFile(string path)
    {
        string json = JsonUtility.ToJson(m_JPD);
        File.WriteAllText(path, json);
    }

    public static void ParseJson(string path)
    {
        //TextAsset textAsset = Resources.Load<TextAsset>(path);
        // => Asset/Resource 산하가 아닌 외부 경로의 json 파일 읽기
        string json = File.ReadAllText(path);
        JPD jpd = JsonUtility.FromJson<JPD>(json);
    }
}
