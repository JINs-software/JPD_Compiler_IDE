using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class JpdUI : UI_Base
{
    enum Toggles
    {
        NamespaceDefineToggle,
        EnumDefineToggle,
        ConstDefineToggle,
    }

    Toggle namespaceDefineToggle;
    Toggle enumDefineToggle;
    Toggle constDefineToggle;

    GameObject ActiveDefineUI = null;

    private void Start()
    {
        Bind<Toggle>(typeof(Toggles));

        namespaceDefineToggle = GetToggle((int)Toggles.NamespaceDefineToggle);
        enumDefineToggle = GetToggle((int)Toggles.EnumDefineToggle);
        constDefineToggle = GetToggle((int)Toggles.ConstDefineToggle);

        BindEvent(namespaceDefineToggle.gameObject, OnNamespaceDefineToggleClicked);
        BindEvent(enumDefineToggle.gameObject, OnEnumDefineToggleClicked);
        BindEvent(constDefineToggle.gameObject, OnConstDefineToggleClicked);

        namespaceDefineToggle.isOn = true;
        ActiveDefineUI = LoadUI(Toggles.NamespaceDefineToggle);
    }

    void OnNamespaceDefineToggleClicked(PointerEventData eventData)
    {
        if (namespaceDefineToggle.isOn)
        {
            enumDefineToggle.isOn = false;
            constDefineToggle.isOn = false;

            if(ActiveDefineUI != null)
            {
                GameObject.Destroy(ActiveDefineUI);
            }
            ActiveDefineUI = LoadUI(Toggles.NamespaceDefineToggle);
        }
        else
        {
            namespaceDefineToggle.isOn = true;
        }
    }

    void OnEnumDefineToggleClicked(PointerEventData eventData)
    {
        if (enumDefineToggle.isOn)
        {
            namespaceDefineToggle.isOn = false;
            constDefineToggle.isOn = false;

            if (ActiveDefineUI != null)
            {
                GameObject.Destroy(ActiveDefineUI);
            }
            ActiveDefineUI = LoadUI(Toggles.EnumDefineToggle);
        }
        else
        {
            enumDefineToggle.isOn = true;
        }
    }
    void OnConstDefineToggleClicked(PointerEventData eventData)
    {
        if (constDefineToggle.isOn)
        {
            namespaceDefineToggle.isOn = false;
            enumDefineToggle.isOn = false;

            if (ActiveDefineUI != null)
            {
                GameObject.Destroy(ActiveDefineUI);
            }
            ActiveDefineUI = LoadUI(Toggles.ConstDefineToggle);
        }
        else
        {
            constDefineToggle.isOn = true;
        }
    }

    private GameObject LoadUI(Toggles toggles)
    {
        string prefabName = string.Empty; 
        if(toggles == Toggles.NamespaceDefineToggle)
        {
            prefabName = "NamespaceDefineUI";
        }
        else if (toggles == Toggles.EnumDefineToggle)
        {
            prefabName = "EnumDefineUI";
        }
        else if (toggles == Toggles.ConstDefineToggle)
        {
            prefabName = "ConstDefineUI";
        }

        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : Prefabs/" + prefabName);
            return null;
        }
        return Object.Instantiate(prefab, transform);
    }
}