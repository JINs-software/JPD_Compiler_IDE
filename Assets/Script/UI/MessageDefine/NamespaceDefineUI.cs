using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NamespaceDefineUI : UI_Base
{
    enum Buttons
    {
        ValidBtn,
        NewBtn,
        OkBtn,
        AddBtn,
    }

    enum Toggles
    {
        SimpleHdrToggle,
        EnDecodeToggle,
    }

    enum Dropdowns
    {
        NamespaceDropdown,
    }

    enum InputFields
    {
        ValidCodeInput,
        NamespaceInput,
        IdInput,
    }

    enum ScrollRects
    {
        MessageBlockView,
    }

    JPD_NAMESPACE ActiveJpdNamespace = null;
    List<MessageBlock> MessageBlocks = new List<MessageBlock>();    
    MessageDefineUI ActiveMessageDefineUI = null;

    private void Start()
    {
        Bind<Button>(typeof(Buttons));
        Bind<Toggle>(typeof(Toggles));
        Bind<Dropdown>(typeof(Dropdowns));
        Bind<InputField>(typeof(InputFields));
        Bind<ScrollRect>(typeof(ScrollRects));

        Button validBtn = GetButton((int)Buttons.ValidBtn);
        Button newBtn = GetButton((int)Buttons.NewBtn);
        Button okBtn = GetButton((int)Buttons.OkBtn);
        Button addBtn = GetButton((int)Buttons.AddBtn);

        Toggle simpleHdrToggle = GetToggle((int)Toggles.SimpleHdrToggle);
        Toggle endecodeToggle = GetToggle((int)Toggles.EnDecodeToggle);

        Dropdown namespaceDropdown = GetDropdown((int)Dropdowns.NamespaceDropdown);

        InputField validCodeInput = GetInputField((int)InputFields.ValidCodeInput);
        InputField namespaceInput = GetInputField((int)InputFields.NamespaceInput);
        InputField idInput = GetInputField((int)InputFields.IdInput);

        //BindEvent(namespaceDropdown.gameObject, OnNamespaceDropdownClicked, Define.UIEvent.Click);
        // => 드롭다운을 클릭할 때는 이벤트를 받지만, 클릭 후 나열된 옵션들을 선택할 땐 이벤트를 받지 못하는 문제 발생
        namespaceDropdown.onValueChanged.AddListener(OnNamespaceChanged);

        BindEvent(validBtn.gameObject, OnValidBtnClicked, Define.UIEvent.Click);
        BindEvent(newBtn.gameObject, OnNewBtnClicked, Define.UIEvent.Click);
        BindEvent(okBtn.gameObject, OnOkBtnClicked, Define.UIEvent.Click);
        BindEvent(addBtn.gameObject, OnAddBtnClicked, Define.UIEvent.Click);

        BindEvent(simpleHdrToggle.gameObject, OnSimpleHdrToggleClicked, Define.UIEvent.Click);
        BindEvent(endecodeToggle.gameObject, OnEnDecodeToggleClicked, Define.UIEvent.Click);

        // 초기 화면
        simpleHdrToggle.isOn = false;
        endecodeToggle.isOn = true;
        JPDCompiler.SimpleHdrMode = false;
        JPDCompiler.EnDecodeFlag = true;

        // 초기 화면 (OK 상태)
        newBtn.interactable = false;
        okBtn.interactable = true;
        addBtn.interactable = false;
        namespaceDropdown.interactable = false;
        namespaceInput.interactable = true;
        idInput.interactable = true;

        ResetNamespaceDropdown();

        if (JPDCompiler.JPDSchema.JPD_Namespaces.Count > 0)
        {
            ActiveJpdNamespace = JPDCompiler.JPDSchema.JPD_Namespaces[0];
            ResetView();

            namespaceDropdown.interactable = true;
            newBtn.interactable = true;
            addBtn.interactable = true;
        }
    }

    private void OnSimpleHdrToggleClicked(PointerEventData data)
    {
        Toggle simpleHdrToggle = GetToggle((int)Toggles.SimpleHdrToggle);
        Toggle endecodeToggle = GetToggle((int)Toggles.EnDecodeToggle);
        if (simpleHdrToggle.isOn)
        {
            endecodeToggle.interactable = false;
            JPDCompiler.SimpleHdrMode = true;
            JPDCompiler.EnDecodeFlag = false;
        }
        else
        {
            endecodeToggle.interactable = true;
            JPDCompiler.SimpleHdrMode = false;
        }
    }

    private void OnEnDecodeToggleClicked(PointerEventData data)
    {
        Toggle endecodeToggle = GetToggle((int)Toggles.EnDecodeToggle);
        if (endecodeToggle.isOn)
        {
            JPDCompiler.EnDecodeFlag = true;
        }
        else
        {
            JPDCompiler.EnDecodeFlag = false;
        }
    }

    public void OnNamespaceChanged(int newIndex)
    {
        Dropdown namespaceDropdown = GetDropdown((int)Dropdowns.NamespaceDropdown);
        string namespaceName = namespaceDropdown.options[newIndex].text;

        if (ActiveJpdNamespace == null || ActiveJpdNamespace.Namespace != namespaceName)
        {
            foreach (var ns in JPDCompiler.JPDSchema.JPD_Namespaces)
            {
                if (ns.Namespace == namespaceName)
                {
                    ActiveJpdNamespace = ns;
                    ResetView();
                    if (ActiveMessageDefineUI != null)
                    {
                        GameObject.Destroy(ActiveMessageDefineUI);
                    }

                    break;
                }
            }
        }
    }

    public void OnValidBtnClicked(PointerEventData eventdata)
    {
        InputField validInput = GetInputField((int)InputFields.ValidCodeInput);
        string validCodeStr = validInput.text;
        byte validCode = byte.Parse(validCodeStr);
        JPDCompiler.ValidCode = validCode;
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

        ClearNamspace();
    }
    public void OnOkBtnClicked(PointerEventData eventdata)
    {
        Debug.Log("OnOkBtnClicked");
        string namespaceName = GetInputField((int)InputFields.NamespaceInput).text;
        string id = GetInputField((int)InputFields.IdInput).text;

        if (ActiveJpdNamespace != null)
        {
            // Namespace 이름 갱신
            foreach(var jpdNamespace in JPDCompiler.JPDSchema.JPD_Namespaces)
            {
                if(ActiveJpdNamespace == jpdNamespace)
                {
                    jpdNamespace.Namespace = GetInputField((int)InputFields.NamespaceInput).text;
                    jpdNamespace.ID = GetInputField((int)InputFields.IdInput).text;
                }
            }
        }
        else
        {
            // 새로운 Namespace 생성
            ActiveJpdNamespace = new JPD_NAMESPACE(GetInputField((int)InputFields.NamespaceInput).text, GetInputField((int)InputFields.IdInput).text);
            JPDCompiler.JPDSchema.JPD_Namespaces.Add(ActiveJpdNamespace);

            GetInputField((int)InputFields.NamespaceInput).interactable = false;
            GetButton((int)Buttons.NewBtn).interactable = true;
            GetButton((int)Buttons.AddBtn).interactable = true;
            GetButton((int)Buttons.OkBtn).interactable = true;

            ResetNamespaceDropdown();
        }
    }

    public void OnAddBtnClicked(PointerEventData eventdata)
    {
        JPD_MESSAGE newJpdMessage = new JPD_MESSAGE();
        ActiveJpdNamespace.Defines.Add(newJpdMessage);
        CreateAndAddJpdMsgBlock(newJpdMessage);
    }

    private void ResetNamespaceDropdown()
    {
        Dropdown namespaceDropdown = GetDropdown((int)Dropdowns.NamespaceDropdown);
        List<Dropdown.OptionData> optionList = new List<Dropdown.OptionData>();
        foreach (var jpdNamspace in JPDCompiler.JPDSchema.JPD_Namespaces)
        {
            optionList.Add(new Dropdown.OptionData(jpdNamspace.Namespace));
        }

        namespaceDropdown.options = optionList;
    }

    private void ResetView()
    {
        if (ActiveJpdNamespace != null)
        {
            ClearNamspace();

            GetDropdown((int)Dropdowns.NamespaceDropdown).value = GetOptionIndex(ActiveJpdNamespace.Namespace);
            GetInputField((int)InputFields.NamespaceInput).text = ActiveJpdNamespace.Namespace;
            GetInputField((int)InputFields.IdInput).text = ActiveJpdNamespace.ID;

            MessageBlocks.Clear();

            foreach (var msg in ActiveJpdNamespace.Defines)
            {
                MessageBlock msgBlock = CreateAndAddJpdMsgBlock(msg);
                msgBlock.ResetBlockText();
            }
        }
    }

    private void ClearNamspace()
    {
        ScrollRect jpdViewRect = Get<ScrollRect>((int)ScrollRects.MessageBlockView);
        GameObject scrollViewContent = Util.FindChild(jpdViewRect.gameObject, "Content", true);

        foreach (Transform child in scrollViewContent.transform)
        {
            MessageBlock constBlock = child.GetComponent<MessageBlock>();
            if (constBlock != null)
            {
                Destroy(constBlock.gameObject);
            }
        }

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

    private MessageBlock CreateAndAddJpdMsgBlock(JPD_MESSAGE jpdMessage)
    {
        ScrollRect jpdViewRect = Get<ScrollRect>((int)ScrollRects.MessageBlockView);
        GameObject scrollViewContent = Util.FindChild(jpdViewRect.gameObject, "Content", true);

        GameObject prefab = Resources.Load<GameObject>("Prefabs/MessageBlock");
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : Prefabs/MessageBlock");
            return null;
        }
        GameObject msgBlockObj = Object.Instantiate(prefab);//, gameObject.transform.parent);

        GetButton((int)Buttons.AddBtn).gameObject.transform.SetParent(null);
        msgBlockObj.transform.SetParent(scrollViewContent.transform);
        GetButton((int)Buttons.AddBtn).gameObject.transform.SetParent(scrollViewContent.transform);
        Canvas.ForceUpdateCanvases();
        jpdViewRect.verticalNormalizedPosition = 0;

        MessageBlock msgBlock = msgBlockObj.GetComponent<MessageBlock>();
        msgBlock.JpdMessage = jpdMessage;   
        msgBlock.OnClickHandler += OnMessageBlockClicked;
        msgBlock.OnDeleteHandler += OnMessageBlockDelete;

        MessageBlocks.Add(msgBlock);

        return msgBlock;
    }

    private void OnMessageBlockClicked(MessageBlock block)
    {
        if(ActiveMessageDefineUI != null)
        {
            GameObject.Destroy(ActiveMessageDefineUI.gameObject);
        }

        GameObject prefab = Resources.Load<GameObject>("Prefabs/MessageDefineUI");
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : Prefabs/MessageDefineUI");
            return;
        }

        ActiveMessageDefineUI = Object.Instantiate(prefab, transform.parent).GetComponent<MessageDefineUI>();
        ActiveMessageDefineUI.MessageBlock = block; 
        ActiveMessageDefineUI.JpdMessage = block.JpdMessage;
    }

    private void OnMessageBlockDelete(MessageBlock block)
    {
        if (ActiveMessageDefineUI != null)
        {
            GameObject.Destroy(ActiveMessageDefineUI.gameObject);
        }
        MessageBlocks.Remove(block);
        ResetJpdNamespace();
    }

    private void ResetJpdNamespace()
    {
        if(ActiveJpdNamespace != null)
        {
            ActiveJpdNamespace.Defines.Clear();
            foreach (var msgBlock in MessageBlocks)
            {
                ActiveJpdNamespace.Defines.Add(msgBlock.JpdMessage);
            }
        }
    }
}
