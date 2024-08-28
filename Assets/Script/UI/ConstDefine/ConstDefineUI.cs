using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ConstDefineUI : UI_Base
{
    enum Buttons
    {
        NewBtn,
        OkBtn,
        AddBtn,
    }
    enum Dropdowns
    {
        ConstGroupListDropdown,
    }
    enum InputFields
    {
        ConstGroupInput,
    }
    enum ScrollRects
    {
        ConstBlockView,
    }

    string ActiveConstGroup = string.Empty;
    Dictionary<string, List<ConstBlock>> ConstBlocks = new Dictionary<string, List<ConstBlock>>();

    private void Start()
    {
        Bind<Button>(typeof(Buttons));
        Bind<Dropdown>(typeof(Dropdowns));
        Bind<InputField>(typeof(InputFields));
        Bind<ScrollRect>(typeof(ScrollRects));

        Dropdown constGroupListDropdown = GetDropdown((int)Dropdowns.ConstGroupListDropdown);
        constGroupListDropdown.interactable = false;
        Button newBtn = GetButton((int)Buttons.NewBtn);
        newBtn.interactable = false;
        Button okBtn = GetButton((int)Buttons.OkBtn);
        okBtn.interactable = true;
        Button addBtn = GetButton((int)Buttons.AddBtn);
        addBtn.interactable = false;

        constGroupListDropdown.onValueChanged.AddListener(OnConstGroupListChanged);

        BindEvent(newBtn.gameObject, OnNewBtnClicked);
        BindEvent(okBtn.gameObject, OnOkBtnClicked);
        BindEvent(addBtn.gameObject, OnAddBtnClicked);

        ResetCosntGroupListDropDown();
        if (JPDCompiler.JPDSchema.JPD_ConstGroups.Count > 0)
        {
            ActiveConstGroup = JPDCompiler.JPDSchema.JPD_ConstGroups[0].Name;
            ResetConstView(JPDCompiler.JPDSchema.JPD_ConstGroups[0]);

            constGroupListDropdown.interactable = true;
            newBtn.interactable = true;
            addBtn.interactable = true;
        }
    }

    private void OnNewBtnClicked(PointerEventData data)
    {
        GetDropdown((int)Dropdowns.ConstGroupListDropdown).interactable = false;
        GetButton((int)Buttons.NewBtn).interactable = false;
        GetButton((int)Buttons.OkBtn).interactable = true;
        GetButton((int)Buttons.AddBtn).interactable = false;
        GetInputField((int)InputFields.ConstGroupInput).text = "";
        ClearConstView();
        ActiveConstGroup = null;
    }

    private void OnOkBtnClicked(PointerEventData data)
    {
        GetButton((int)Buttons.NewBtn).interactable = true;

        string constGroup = GetInputField((int)InputFields.ConstGroupInput).text;
        if (string.IsNullOrEmpty(constGroup))
        {
            return;
        }

        if (string.IsNullOrEmpty(ActiveConstGroup))
        {
            foreach (var jpdConstGroup in JPDCompiler.JPDSchema.JPD_ConstGroups)
            {
                if (jpdConstGroup.Name == constGroup)
                {
                    return;
                }
            }

            ConstBlocks.Add(constGroup, new List<ConstBlock>());

            JPDCompiler.JPDSchema.JPD_ConstGroups.Add(new JPD_CONST_GROUP(constGroup));

            List<Dropdown.OptionData> optionDatas = new List<Dropdown.OptionData>();
            optionDatas.Add(new Dropdown.OptionData(constGroup));

            GetDropdown((int)Dropdowns.ConstGroupListDropdown).AddOptions(optionDatas);
            GetDropdown((int)Dropdowns.ConstGroupListDropdown).value = GetDropdown((int)Dropdowns.ConstGroupListDropdown).options.Count - 1;
        }
        else
        {
            foreach (var jpdConstGroup in JPDCompiler.JPDSchema.JPD_ConstGroups)
            {
                if (jpdConstGroup.Name == ActiveConstGroup)
                {
                    ConstBlocks.Add(constGroup, ConstBlocks[ActiveConstGroup]);
                    ConstBlocks.Remove(ActiveConstGroup);
                    jpdConstGroup.Name = constGroup;
                    GetDropdown((int)Dropdowns.ConstGroupListDropdown).options.ForEach(optionData => { optionData.text = constGroup; });
                    GetDropdown((int)Dropdowns.ConstGroupListDropdown).RefreshShownValue();
                }
            }
        }

        ActiveConstGroup = constGroup;
        GetDropdown((int)Dropdowns.ConstGroupListDropdown).interactable = true;
        GetButton((int)Buttons.NewBtn).interactable = true;
        GetButton((int)Buttons.OkBtn).interactable = true;
        GetButton((int)Buttons.AddBtn).interactable = true;
    }

    private void OnAddBtnClicked(PointerEventData data)
    {
        foreach (var jpdConstGroup in JPDCompiler.JPDSchema.JPD_ConstGroups)
        {
            if (jpdConstGroup.Name == ActiveConstGroup)
            {
                CreateAndAddConstBlock(ActiveConstGroup);
            }
        }
    }

    public void OnConstGroupListChanged(int newIndex)
    {
        Dropdown constGroups = GetDropdown((int)Dropdowns.ConstGroupListDropdown);
        string constGroupName = constGroups.options[newIndex].text;
        if (string.IsNullOrEmpty(ActiveConstGroup) || ActiveConstGroup != constGroupName)
        {
            foreach (var cg in JPDCompiler.JPDSchema.JPD_ConstGroups)
            {
                if (cg.Name == constGroupName)
                {
                    ActiveConstGroup = constGroupName;
                    GetInputField((int)InputFields.ConstGroupInput).text = cg.Name;
                    ResetConstView(cg);
                    break;
                }
            }
        }
    }

    private void ResetConstView(JPD_CONST_GROUP cg)
    {
        if (!string.IsNullOrEmpty(ActiveConstGroup))
        {
            ClearConstView();

            foreach(var con in cg.Consts)
            {
                ConstBlock constBlock = CreateAndAddConstBlock(ActiveConstGroup);
                if (constBlock != null)
                {
                    constBlock.typeInput.text = con.Type;
                    constBlock.nameInput.text = con.Name;
                    constBlock.valueInput.text = con.Value;
                }
            }
        }
    }

    private ConstBlock CreateAndAddConstBlock(string constGroup)
    {
        if (!ConstBlocks.ContainsKey(constGroup))
        {
            ConstBlocks.Add(constGroup, new List<ConstBlock>());
        }

        ScrollRect jpdViewRect = Get<ScrollRect>((int)ScrollRects.ConstBlockView);
        GameObject scrollViewContent = Util.FindChild(jpdViewRect.gameObject, "Content", true);

        GameObject prefab = Resources.Load<GameObject>("Prefabs/ConstBlock");
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : Prefabs/ConstBlock");
            return null;
        }
        GameObject constBlockObj = Object.Instantiate(prefab);

        GetButton((int)Buttons.AddBtn).gameObject.transform.SetParent(null);
        constBlockObj.transform.SetParent(scrollViewContent.transform);
        GetButton((int)Buttons.AddBtn).gameObject.transform.SetParent(scrollViewContent.transform);
        Canvas.ForceUpdateCanvases();
        jpdViewRect.verticalNormalizedPosition = 0;

        ConstBlock constBlock = constBlockObj.GetComponent<ConstBlock>();
        constBlock.ConstGroup = constGroup;
        constBlock.ConstBlockChangeHandler += OnConstBlockChange;
        constBlock.CancelHandler += OnConstBlockCancel;

        ConstBlocks[constGroup].Add(constBlock);

        return constBlock;
    }

    private void OnConstBlockChange(string constGroup)
    {
        ResetJpdConsts(constGroup);
    }

    private void OnConstBlockCancel(string constGroup, ConstBlock block)
    {
        ConstBlocks[constGroup].Remove(block);
        GameObject.Destroy(block.gameObject);
        ResetJpdConsts(constGroup);
    }

    private void ResetJpdConsts(string constGroup)
    {
        foreach (var jpdConstGroup in JPDCompiler.JPDSchema.JPD_ConstGroups)
        {
            if (jpdConstGroup.Name == constGroup)
            {
                jpdConstGroup.Consts.Clear();

                foreach (var constBlock in ConstBlocks[constGroup])
                {
                    JPD_CONST jpdConst = new JPD_CONST();
                    jpdConst.Type = constBlock.typeInput.text;
                    jpdConst.Name = constBlock.nameInput.text;
                    jpdConst.Value = constBlock.valueInput.text;
                    jpdConstGroup.Consts.Add(jpdConst);
                }
            }
        }
    }

    private void ClearConstView()
    {
        ScrollRect jpdViewRect = Get<ScrollRect>((int)ScrollRects.ConstBlockView);
        GameObject scrollViewContent = Util.FindChild(jpdViewRect.gameObject, "Content", true);

        foreach (Transform child in scrollViewContent.transform)
        {
            ConstBlock constBlock = child.GetComponent<ConstBlock>();
            if (constBlock != null)
            {
                Destroy(constBlock.gameObject);
            }
        }
    }

    private void ResetCosntGroupListDropDown()
    {
        Dropdown constGroupList = GetDropdown((int)Dropdowns.ConstGroupListDropdown);
        List<Dropdown.OptionData> optionList = new List<Dropdown.OptionData>();
        foreach (var jpdConstGroup in JPDCompiler.JPDSchema.JPD_ConstGroups)
        {
            optionList.Add(new Dropdown.OptionData(jpdConstGroup.Name));
        }

        constGroupList.options = optionList;
    }
}
