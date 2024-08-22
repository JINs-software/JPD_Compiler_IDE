using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class JRpcUI : UI_Base
{
    enum Buttons
    {
        NewBtn,
        OkBtn,
        AddBtn,
    }

    enum Dropdowns
    {
        NamespaceDropdown,
    }

    enum InputFields
    {
        NamespaceInput,
        IdInput,
    }
    
    enum ScrollRects
    {
        JPD_VIEW,
    }

    //Dictionary<string, JPD_NAMESPACE> JpdNamespaces = new Dictionary<string, JPD_NAMESPACE>();
    //Dictionary<string, Dictionary<Button, JPD_MESSAGE>> JpdMessages = new Dictionary<string, Dictionary<Button, JPD_MESSAGE>>();
    //
    //string ActiveNamespace = string.Empty;
    //GameObject ActiveJpdDefineUI = null;

    public JPD_SCHEMA JpdSchema = null;
    JPD_NAMESPACE ActiveJpdNamespace = null;
    Dictionary<Button, JPD_MESSAGE> JpdMessageButtons = new Dictionary<Button, JPD_MESSAGE>();
    GameObject ActiveMessageDefineUI = null;    

    private void Start()
    {
        Bind<Button>(typeof(Buttons));
        Bind<Dropdown>(typeof(Dropdowns));
        Bind<InputField>(typeof(InputFields));
        Bind<ScrollRect>(typeof(ScrollRects));   

        Button newBtn = GetButton((int)Buttons.NewBtn);
        Button okBtn = GetButton((int)Buttons.OkBtn);
        Button addBtn = GetButton((int)Buttons.AddBtn);
       
        Dropdown namespaceDropdown = GetDropdown((int)Dropdowns.NamespaceDropdown);
        
        InputField namespaceInput = GetInputField((int)InputFields.NamespaceInput);
        InputField idInput = GetInputField((int)InputFields.IdInput);

        ScrollRect jpdView = Get<ScrollRect>((int)ScrollRects.JPD_VIEW);

        //BindEvent(namespaceDropdown.gameObject, OnNamespaceDropdownClicked, Define.UIEvent.Click);
        // => 드롭다운을 클릭할 때는 이벤트를 받지만, 클릭 후 나열된 옵션들을 선택할 땐 이벤트를 받지 못하는 문제 발생
        namespaceDropdown.onValueChanged.AddListener(OnNamespaceChanged);

        BindEvent(newBtn.gameObject, OnNewBtnClicked, Define.UIEvent.Click);
        BindEvent(okBtn.gameObject, OnOkBtnClicked, Define.UIEvent.Click);
        BindEvent(addBtn.gameObject, OnAddBtnClicked, Define.UIEvent.Click);

        // 초기 화면 (OK 상태)
        newBtn.interactable = false;
        okBtn.interactable = true;
        addBtn.interactable = false;
        namespaceDropdown.interactable = true;
        namespaceInput.interactable = true;
        idInput.interactable = false;

        // JPD_SCHEMA 기반 설정
        JpdSchema = JPDCompiler.JPDSchema;

        ResetNamespaceDropdown();

        if(JpdSchema.JPD.Count > 0)
        {
            ActiveJpdNamespace = JpdSchema.JPD[0];
            ResetView();
        }
    }

    public void OnNamespaceChanged(int newIndex)
    {
        Dropdown namespaceDropdown = GetDropdown((int)Dropdowns.NamespaceDropdown);
        string namespaceName = namespaceDropdown.options[newIndex].text;

        if (ActiveJpdNamespace == null || ActiveJpdNamespace.Namespace != namespaceName) 
        {
            foreach(var ns in JpdSchema.JPD)
            {
                if(ns.Namespace == namespaceName)
                {
                    ActiveJpdNamespace = ns;
                    ResetView();
                    if(ActiveMessageDefineUI != null)
                    {
                        GameObject.Destroy(ActiveMessageDefineUI);
                    }

                    break;
                }
            }
        }
    }

    public void OnNewBtnClicked(PointerEventData eventdata)
    {
        Debug.Log("OnNewBtnClicked");
        GetButton((int)Buttons.NewBtn).interactable = false;
        GetButton((int)Buttons.AddBtn).interactable = false;
        GetButton((int)Buttons.OkBtn).interactable = true;
        GetDropdown((int)Dropdowns.NamespaceDropdown).interactable = true;
        GetInputField((int)InputFields.NamespaceInput).interactable = true;
        GetInputField((int)InputFields.IdInput).interactable = true;

        ClearView();
    }
    public void OnOkBtnClicked(PointerEventData eventdata)
    {
        Debug.Log("OnOkBtnClicked");
        string namespaceName = GetInputField((int)InputFields.NamespaceInput).text;
        string id = GetInputField((int)InputFields.IdInput).text;

        JPD_NAMESPACE jpdItem = JPDCompiler.Instance.AddJpdNamespace(namespaceName, id);
        if (jpdItem != null)
        {
            GetInputField((int)InputFields.NamespaceInput).interactable = false;
            GetInputField((int)InputFields.IdInput).text = jpdItem.ID;

            GetButton((int)Buttons.NewBtn).interactable = true;
            GetButton((int)Buttons.AddBtn).interactable = true;
            GetButton((int)Buttons.OkBtn).interactable = false;

            ResetNamespaceDropdown();
            ClearView();
            ActiveJpdNamespace = jpdItem;
        }

    }
    public void OnAddBtnClicked(PointerEventData eventdata)
    {
        JPD_MESSAGE newJpdMessage = new JPD_MESSAGE();
        ActiveJpdNamespace.Defines.Add(new JPD_MESSAGE());
        CreateAndAddJpdMsgBlock(newJpdMessage);
    }

    public void OnJpdMsgBlockClicked(PointerEventData eventdata)
    {
        Button jpdMsgBtnBlock = eventdata.selectedObject.GetComponent<Button>();

        if (ActiveMessageDefineUI != null)
        {
            GameObject.Destroy(ActiveMessageDefineUI);
        }

        GameObject prefab = Resources.Load<GameObject>("Prefabs/JRpcDefGroup");
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : Prefabs/JRpcDefGroup");
            return;
        }
        
        ActiveMessageDefineUI = Object.Instantiate(prefab, gameObject.transform.parent);
        ActiveMessageDefineUI.GetComponent<JRpcDefineUI>().JpdBlockBtn = jpdMsgBtnBlock;
        ActiveMessageDefineUI.GetComponent<JRpcDefineUI>().JpdMessage = JpdMessageButtons[jpdMsgBtnBlock];
    }

    private void ResetNamespaceDropdown()
    {
        Dropdown namespaceDropdown = GetDropdown((int)Dropdowns.NamespaceDropdown);
        List<string> options = new List<string>();
        foreach (var jpdNamespace in JpdSchema.JPD)
        {
            options.Add(jpdNamespace.Namespace);
        }
        namespaceDropdown.ClearOptions();
        namespaceDropdown.AddOptions(options);
    }

    private void ResetView()
    {
        if (ActiveJpdNamespace != null)
        {
            ClearView();
            
            GetDropdown((int)Dropdowns.NamespaceDropdown).value = GetOptionIndex(ActiveJpdNamespace.Namespace);
            GetInputField((int)InputFields.NamespaceInput).text = ActiveJpdNamespace.Namespace;
            GetInputField((int)InputFields.IdInput).text = ActiveJpdNamespace.ID;

            JpdMessageButtons.Clear();
            foreach (var msg in ActiveJpdNamespace.Defines)
            {
                CreateAndAddJpdMsgBlock(msg);
            }

            GetButton((int)Buttons.AddBtn).interactable = true; 
        }
    }

    private void DeleteFromView(Button jpdMsgBtn)
    {
        if (JpdMessageButtons.ContainsKey(jpdMsgBtn))
        {
            jpdMsgBtn.gameObject.transform.parent = null;
            JpdMessageButtons.Remove(jpdMsgBtn);
            Object.Destroy(jpdMsgBtn.gameObject);
        }
    }
    private void ClearView()
    {
        // 기존 메시지 버튼 정리
        foreach (var msgBtn in JpdMessageButtons)
        {
            Button btn = msgBtn.Key;
            btn.gameObject.transform.parent = null;
            Object.Destroy(btn.gameObject);
        }
        JpdMessageButtons.Clear();
        
        GetInputField((int)InputFields.NamespaceInput).text = "";
        GetInputField((int)InputFields.IdInput).text = "";
    }

    private int GetOptionIndex(string option)
    {
        Dropdown namespaceDropdown = GetDropdown((int)Dropdowns.NamespaceDropdown);
        // 모든 옵션을 순회
        for (int i = 0; i < namespaceDropdown.options.Count; i++)
        {
            if (namespaceDropdown.options[i].text == option) // 문자열 비교
            {
                return i; // 일치하는 인덱스 반환
            }
        }
        return -1; // 문자열이 없으면 -1 반환
    }

    private GameObject CreateAndAddJpdMsgBlock(JPD_MESSAGE msg)
    {
        ScrollRect jpdViewRect = Get<ScrollRect>((int)ScrollRects.JPD_VIEW);
        GameObject scrollViewContent = Utill.FindChild(jpdViewRect.gameObject, "Content", true);

        GameObject newMsgBtnBlock = CreateJpdMsgBlock();
        Button msgBtn = newMsgBtnBlock.GetComponent<Button>();
        BindEvent(newMsgBtnBlock, OnJpdMsgBlockClicked, Define.UIEvent.Click);

        Button delBtn = Utill.FindChild(newMsgBtnBlock, "DelBtn", true).GetComponent<Button>();
        BindEvent(delBtn.gameObject, OnJpdMsgBlockDelBtnClicked, Define.UIEvent.Click);

        JpdMessageButtons.Add(msgBtn, msg);
        
        GetButton((int)Buttons.AddBtn).gameObject.transform.SetParent(null);
        newMsgBtnBlock.transform.SetParent(scrollViewContent.transform);
        GetButton((int)Buttons.AddBtn).gameObject.transform.SetParent(scrollViewContent.transform);
        Canvas.ForceUpdateCanvases();
        jpdViewRect.verticalNormalizedPosition = 0;

        SetJpdMessageBtntext(newMsgBtnBlock, msg);
        
        return newMsgBtnBlock;
    }

    private void OnJpdMsgBlockDelBtnClicked(PointerEventData eventdata)
    {
        GameObject delBtnObj = eventdata.selectedObject;
        GameObject msgBlock = delBtnObj.transform.parent.gameObject;
        Button msgBlockBtn = msgBlock.GetComponent<Button>();
        JPD_MESSAGE jpdMsg = JpdMessageButtons[msgBlockBtn];
        //JpdMessageButtons.Remove(msgBlockBtn);
        ActiveJpdNamespace.Defines.Remove(jpdMsg);
        if (ActiveMessageDefineUI != null)
        {
            GameObject.Destroy(ActiveMessageDefineUI);
        }
        DeleteFromView(msgBlockBtn);
        GameObject.Destroy(msgBlockBtn.gameObject);
        ResetView();
    }

    private GameObject CreateJpdMsgBlock()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/JPD_Block");
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : Prefabs/JPD_Block");
            return null;
        }
        GameObject jpdBlock = Object.Instantiate(prefab);//, gameObject.transform.parent);

        return jpdBlock;
    }

    public static void SetJpdMessageBtntext(GameObject jpdMsgBtnBlock, JPD_MESSAGE msg)
    {
        if(string.IsNullOrEmpty(msg.Message) || string.IsNullOrEmpty(msg.Dir))
        {
            return;
        }

        Text messageText = Utill.FindChild(jpdMsgBtnBlock, "MessageText").GetComponent<Text>();
        messageText.text = "";

        messageText.text = msg.Message + "_" + msg.Dir;
        messageText.text += "\r\n";
        messageText.text += "(";

        foreach (var param in msg.Param)
        {
            messageText.text += param.Type + " " + param.Name + ", ";
        }
        if (msg.Param.Count > 0)
        {
            messageText.text = messageText.text.Substring(0, messageText.text.Length - 2);
        }

        messageText.text += ")";
    }
}
