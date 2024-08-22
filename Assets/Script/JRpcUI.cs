using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    public JPD Jpd = null;
    //Dictionary<int, string> JpdNamespaceDic = new Dictionary<int, string>();
    Dictionary<string, JPD_ITEM> JpdItems = new Dictionary<string, JPD_ITEM>();
    Dictionary<string, Dictionary<Button, JPD_DEFINE>> JpdDefines = new Dictionary<string, Dictionary<Button, JPD_DEFINE>>();

    string ActiveNamespace = string.Empty;
    GameObject ActiveJpdDefineUI = null;

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
    }

    public void OnNewBtnClicked(PointerEventData eventdata)
    {
        Debug.Log("OnNewBtnClicked");
        GetButton((int)Buttons.NewBtn).interactable = false;
        GetButton((int)Buttons.AddBtn).interactable = false;
        GetButton((int)Buttons.OkBtn).interactable = true;
        GetDropdown((int)Dropdowns.NamespaceDropdown).interactable = true;
        GetInputField((int)InputFields.NamespaceInput).interactable = true;
        GetInputField((int)InputFields.IdInput).interactable = false;

        SaveJpdItem(ActiveNamespace);
    }
    public void OnOkBtnClicked(PointerEventData eventdata)
    {
        Debug.Log("OnOkBtnClicked");
        string namespaceName = GetInputField((int)InputFields.NamespaceInput).text;

        JPD_ITEM jpdItem = JPDCompiler.Instance.AddJpdNamespace(namespaceName);
        if (jpdItem != null)
        {
            GetInputField((int)InputFields.NamespaceInput).interactable = false;
            GetInputField((int)InputFields.IdInput).text = jpdItem.ID;

            GetButton((int)Buttons.NewBtn).interactable = true;
            GetButton((int)Buttons.AddBtn).interactable = true;
            GetButton((int)Buttons.OkBtn).interactable = false;

            //JpdNamespaceDic.Add(jpdItem.ID.To, namespaceName);
            //Jpd.JpdItems
            //JPD_ITEM newJpdItem = new JPD_ITEM(namespaceName, namespaceID.ToString());
            //Jpd.JpdItems.Add(newJpdItem);   
            JpdItems.Add(namespaceName, jpdItem);
            JpdDefines.Add(namespaceName, new Dictionary<Button, JPD_DEFINE>());
            ActiveNamespace = namespaceName;
        }

    }
    public void OnAddBtnClicked(PointerEventData eventdata)
    {
        ScrollRect jpdViewRect = Get<ScrollRect>((int)ScrollRects.JPD_VIEW);
        GameObject scrollViewContent = Utill.FindChild(jpdViewRect.gameObject, "Content", true);
        GameObject newJpdBlock = CreateJpdBlock();
        BindEvent(newJpdBlock, OnJpdBlockClicked, Define.UIEvent.Click);
        JPD_DEFINE newJpdDefine = new JPD_DEFINE();
        JpdItems[ActiveNamespace].Defines.Add(newJpdDefine);
        JpdDefines[ActiveNamespace].Add(newJpdBlock.GetComponent<Button>(), newJpdDefine);

        GetButton((int)Buttons.AddBtn).gameObject.transform.SetParent(null);
        newJpdBlock.transform.SetParent(scrollViewContent.transform);
        GetButton((int)Buttons.AddBtn).gameObject.transform.SetParent(scrollViewContent.transform);

        Canvas.ForceUpdateCanvases();
        jpdViewRect.verticalNormalizedPosition = 0;
    }

    public void OnJpdBlockClicked(PointerEventData eventdata)
    {
        Button jpdBlock = eventdata.selectedObject.GetComponent<Button>();

        if (ActiveJpdDefineUI != null)
        {
            GameObject.Destroy(ActiveJpdDefineUI);
        }

        GameObject prefab = Resources.Load<GameObject>("Prefabs/JRpcDefGroup");
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : Prefabs/JRpcDefGroup");
            return;
        }
        //GameObject defineUI = Object.Instantiate(prefab, gameObject.transform.parent);
        ActiveJpdDefineUI = Object.Instantiate(prefab, gameObject.transform.parent);
        ActiveJpdDefineUI.GetComponent<JRpcDefineUI>().JpdDefine = JpdDefines[ActiveNamespace][jpdBlock];
        ActiveJpdDefineUI.GetComponent<JRpcDefineUI>().JpdBlockBtn = jpdBlock;  
    }

    private GameObject CreateJpdBlock()
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

    private void SaveJpdItem(string namespaceName)
    {
        if (JpdItems.ContainsKey(namespaceName))
        {
            JPD_ITEM jpdItem = JpdItems[namespaceName];
            jpdItem.Defines.Clear();
            //jpdItem
            Dictionary<Button, JPD_DEFINE> defines = JpdDefines[namespaceName];
            foreach(var def in defines)
            {
                jpdItem.Defines.Add(def.Value);
            }
        }
    }
}
