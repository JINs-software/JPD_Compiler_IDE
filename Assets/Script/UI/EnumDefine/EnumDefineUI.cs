using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnumDefineUI : UI_Base
{
    enum Buttons
    {
        NewBtn,
        OkBtn,
        AddBtn,
    }
    enum Dropdowns
    {
        EnumListDropdown,
    }
    enum InputFields
    {
        EnumInput,
    }
    enum ScrollRects
    {
        EnumBlockView,
    }

    string ActiveEnum = string.Empty;
    Dictionary<string, List<EnumBlock>> FieldsDic = new Dictionary<string, List<EnumBlock>>();

    private void Start()
    {
        Bind<Button>(typeof(Buttons));
        Bind<Dropdown>(typeof(Dropdowns));
        Bind<InputField>(typeof(InputFields));
        Bind<ScrollRect>(typeof(ScrollRects));

        Dropdown enumListDropdown = GetDropdown((int)Dropdowns.EnumListDropdown);
        enumListDropdown.interactable = false;
        Button newBtn = GetButton((int)Buttons.NewBtn);
        newBtn.interactable = false;
        Button okBtn = GetButton((int)Buttons.OkBtn);
        okBtn.interactable = true;
        Button addBtn = GetButton((int)Buttons.AddBtn);
        addBtn.interactable = false;

        enumListDropdown.onValueChanged.AddListener(OnEnumListChanged);

        BindEvent(newBtn.gameObject, OnNewBtnClicked);
        BindEvent(okBtn.gameObject, OnOkBtnClicked);
        BindEvent(addBtn.gameObject, OnAddBtnClicked);

        ResetEnumListDropdown();
        if(JPDCompiler.JPDSchema.JPD_Enums.Count > 0 )
        {
            ActiveEnum = JPDCompiler.JPDSchema.JPD_Enums[0].Name;
            ResetEnumView(JPDCompiler.JPDSchema.JPD_Enums[0]);

            enumListDropdown.interactable = true;
            newBtn.interactable = true;
            addBtn.interactable = true;
        }
    }

    public void OnEnumListChanged(int newIndex)
    {
        Dropdown enumList = GetDropdown((int)Dropdowns.EnumListDropdown);
        string enumName = enumList.options[newIndex].text;
        if(string.IsNullOrEmpty(ActiveEnum) ||  ActiveEnum != enumName)
        {
            foreach(var en in JPDCompiler.JPDSchema.JPD_Enums)
            {
                if(en.Name == enumName)
                {
                    ActiveEnum = enumName;
                    GetInputField((int)InputFields.EnumInput).text = en.Name;
                    ResetEnumView(en);
                    break;
                }
            }
        }
    }

    public void OnNewBtnClicked(PointerEventData eventData)
    {
        GetDropdown((int)Dropdowns.EnumListDropdown).interactable = false;
        GetButton((int)Buttons.NewBtn).interactable = false;
        GetButton((int)Buttons.OkBtn).interactable = true;
        GetButton((int)Buttons.AddBtn).interactable = false;
        GetInputField((int)InputFields.EnumInput).text = "";
        ClearEnumView();
        ActiveEnum = null;
    }

    public void OnOkBtnClicked(PointerEventData eventData)
    {
        GetButton((int)Buttons.NewBtn).interactable = true;

        string enumName = GetInputField((int)InputFields.EnumInput).text;
        if (string.IsNullOrEmpty(enumName))
        {
            return;
        }

        if(string.IsNullOrEmpty(ActiveEnum))
        {
            foreach (var jpdEnum in JPDCompiler.JPDSchema.JPD_Enums)
            {
                if (jpdEnum.Name == enumName)
                {
                    return;
                }
            }

            FieldsDic.Add(enumName, new List<EnumBlock>());

            JPDCompiler.JPDSchema.JPD_Enums.Add(new JPD_ENUM(enumName));

            List<Dropdown.OptionData> optionDatas = new List<Dropdown.OptionData>();
            optionDatas.Add(new Dropdown.OptionData(enumName));
            GetDropdown((int)Dropdowns.EnumListDropdown).AddOptions(optionDatas);
            GetDropdown((int)Dropdowns.EnumListDropdown).value = GetDropdown((int)Dropdowns.EnumListDropdown).options.Count - 1;
        }
        else
        {
            foreach (var jpdEnum in JPDCompiler.JPDSchema.JPD_Enums)
            {
                if (jpdEnum.Name == ActiveEnum)
                {
                    FieldsDic.Add(enumName, FieldsDic[ActiveEnum]);
                    FieldsDic.Remove(ActiveEnum);
                    jpdEnum.Name = enumName;
                    GetDropdown((int)Dropdowns.EnumListDropdown).options.ForEach(optionData => { optionData.text = enumName; });
                    GetDropdown((int)Dropdowns.EnumListDropdown).RefreshShownValue();
                }
            }
        }

        ActiveEnum = enumName;
        GetDropdown((int)Dropdowns.EnumListDropdown).interactable = true;
        GetButton((int)Buttons.NewBtn).interactable = true;
        GetButton((int)Buttons.OkBtn).interactable = true;
        GetButton((int)Buttons.AddBtn).interactable = true;
    }

    public void OnAddBtnClicked(PointerEventData eventData)
    {
        foreach(var jpdEnum in JPDCompiler.JPDSchema.JPD_Enums)
        {
            if(jpdEnum.Name == ActiveEnum)
            {
                CreateAndAddEnumBlock(ActiveEnum);
            }
        }
    }

    void OnEnumBlockChange(string enumName)
    {
        ResetJpdEnum(enumName);
    }

    void OnEnumBlockCancel(string enumName, EnumBlock enumBlock)
    {
        FieldsDic[enumName].Remove(enumBlock);
        GameObject.Destroy(enumBlock.gameObject);
        ResetJpdEnum(enumName);
    }

    private void ResetJpdEnum(string enumName)
    {
        foreach (var jpdEnum in JPDCompiler.JPDSchema.JPD_Enums)
        {
            if (jpdEnum.Name == enumName)
            {
                jpdEnum.Fields.Clear();

                foreach(var enumBlock in FieldsDic[enumName])
                {
                    jpdEnum.Fields.Add(enumBlock.fieldInput.text);
                }
            }
        }
    }

    private void ResetEnumView(JPD_ENUM jpdEnum)
    {
        if (!string.IsNullOrEmpty(ActiveEnum))
        {
            ClearEnumView();
            foreach (var field in jpdEnum.Fields)
            {
                EnumBlock enumBlock = CreateAndAddEnumBlock(ActiveEnum);
                enumBlock.fieldInput.text = field;
                
            }
        }
    }

    private EnumBlock CreateAndAddEnumBlock(string enumName)
    {
        if (!FieldsDic.ContainsKey(enumName))
        {
            FieldsDic.Add(enumName, new List<EnumBlock>());
        }

        ScrollRect jpdViewRect = Get<ScrollRect>((int)ScrollRects.EnumBlockView);
        GameObject scrollViewContent = Util.FindChild(jpdViewRect.gameObject, "Content", true);

        GameObject prefab = Resources.Load<GameObject>("Prefabs/EnumBlock");
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : Prefabs/JPD_Block");
            return null;
        }
        GameObject enumBlockObj = Object.Instantiate(prefab);

        GetButton((int)Buttons.AddBtn).gameObject.transform.SetParent(null);
        enumBlockObj.transform.SetParent(scrollViewContent.transform);
        GetButton((int)Buttons.AddBtn).gameObject.transform.SetParent(scrollViewContent.transform);
        Canvas.ForceUpdateCanvases();
        jpdViewRect.verticalNormalizedPosition = 0;

        EnumBlock enumBlock = enumBlockObj.GetComponent<EnumBlock>();
        enumBlock.EnumName = enumName;
        enumBlock.ValueChangedHndler += OnEnumBlockChange;
        enumBlock.CancelHandler += OnEnumBlockCancel;

        FieldsDic[enumName].Add(enumBlock);

        return enumBlock;
    }

    private void ClearEnumView()
    {
        ScrollRect jpdViewRect = Get<ScrollRect>((int)ScrollRects.EnumBlockView);
        GameObject scrollViewContent = Util.FindChild(jpdViewRect.gameObject, "Content", true);

        foreach (Transform child in scrollViewContent.transform)
        {
            // 자식 객체에 AAA 컴포넌트가 있는지 확인합니다.
            EnumBlock enumBlock = child.GetComponent<EnumBlock>();
            if (enumBlock != null)
            {
                Destroy(enumBlock.gameObject);
            }
        }
    }

    private void ResetEnumListDropdown()
    {
        Dropdown enumList = GetDropdown((int)Dropdowns.EnumListDropdown);
        List<Dropdown.OptionData> optionList = new List<Dropdown.OptionData>();
        foreach (var jpdEnum in JPDCompiler.JPDSchema.JPD_Enums)
        {
            optionList.Add(new Dropdown.OptionData(jpdEnum.Name));
        }

        enumList.options = optionList;
    }
}
