using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MessageDefineUI : UI_Base
{
    enum InputFields
    {
        NameInputField,
    }
    enum Toggles
    {
        S2CToggle,
        C2SToggle,
    }
    enum Buttons
    {
        AddParamBtn,
        //OkBtn,
        //DelBtn,
    }
    enum ScrollRects
    {
        PARAM_VIEW,
    }

    public MessageBlock MessageBlock = null;
    public JPD_MESSAGE JpdMessage = null;
    List<ParamBlock> ParamBlocks = new List<ParamBlock>();  

    private void Start()
    {
        Bind<InputField>(typeof(InputFields));
        Bind<Toggle>(typeof(Toggles));
        Bind<Button>(typeof(Buttons));
        Bind<ScrollRect>(typeof(ScrollRects));  

        Toggle s2cToggle = GetToggle((int)Toggles.S2CToggle);
        Toggle c2sToggle = GetToggle((int)Toggles.C2SToggle);
        Button addParamBtn = GetButton((int)Buttons.AddParamBtn);

        BindEvent(s2cToggle.gameObject, OnS2CToggleClicked, Define.UIEvent.Click);
        BindEvent(c2sToggle.gameObject, OnC2SToggleClicked, Define.UIEvent.Click);
        BindEvent(addParamBtn.gameObject, OnAddParamBtnClicked, Define.UIEvent.Click);

        InputField nameInputField = GetInputField((int)InputFields.NameInputField);
        nameInputField.text = JpdMessage.Message;
        nameInputField.onEndEdit.AddListener(OnNameInputEndEdit);

        if (string.IsNullOrEmpty(JpdMessage.Dir))
        {
            JpdMessage.Dir = "S2C";         // 기본 설정
        }
        
        if (JpdMessage.Dir == "S2C")
        {
            s2cToggle.isOn = true;
            c2sToggle.isOn = false;
        }
        else
        {
            s2cToggle.isOn = false;
            c2sToggle.isOn = true;
        }
        

        SetParamView();
    }

    private void OnNameInputEndEdit(string name)
    {
        JpdMessage.Message = name;
        ResetMessageBlockText();
    }

    public void OnS2CToggleClicked(PointerEventData eventdata)
    {
        if (GetToggle((int)Toggles.S2CToggle).isOn)
        {
            JpdMessage.Dir = "S2C";
            GetToggle((int)Toggles.C2SToggle).isOn = false;
            ResetMessageBlockText();
        }
    }

    public void OnC2SToggleClicked(PointerEventData eventdata)
    {
        if (GetToggle((int)Toggles.C2SToggle).isOn)
        {
            JpdMessage.Dir = "C2S";
            GetToggle((int)Toggles.S2CToggle).isOn = false;
            ResetMessageBlockText();
        }
    }

    public void OnAddParamBtnClicked(PointerEventData eventdata)
    {
        CreateAndAddParamBlock();
    }

    private void OnParamBlockChanged()
    {
        ResetJpdMessage();
        ResetMessageBlockText();
    }

    private void OnParamBlockCanceled(ParamBlock block)
    {
        ParamBlocks.Remove(block);
        GameObject.Destroy(block.gameObject);
        ResetJpdMessage();
        ResetMessageBlockText();
    }

    private void ResetJpdMessage()
    {
        JpdMessage.Param.Clear();
        foreach(var paramBlock in ParamBlocks)
        {
            JPD_PARAM jpdParam = new JPD_PARAM();
            jpdParam.Type = paramBlock.TypeInput.text;
            jpdParam.Name = paramBlock.NameInput.text;
            if (paramBlock.ArrayToggle.isOn)
            {
                jpdParam.FixedLenOfArray = paramBlock.FixedLengthInput.text;
            }

            JpdMessage.Param.Add(jpdParam);
        }
    }

    private void ResetMessageBlockText()
    {
        MessageBlock.ResetBlockText();
    }

    private void SetParamView()
    {
        foreach (var param in JpdMessage.Param)
        {
            ParamBlock paramBlock = CreateAndAddParamBlock();
            if(paramBlock != null)
            {
                paramBlock.TypeInput.text = param.Type;
                paramBlock.NameInput.text = param.Name;
                if (!string.IsNullOrEmpty(param.FixedLenOfArray))
                {
                    paramBlock.ArrayToggle.isOn = true;
                    paramBlock.FixedLengthInput.text = param.FixedLenOfArray;       
                }
            }
        }
    }

    private ParamBlock CreateAndAddParamBlock()
    {
        ScrollRect paramViewRect = Get<ScrollRect>((int)ScrollRects.PARAM_VIEW);
        GameObject scrollViewContent = Util.FindChild(paramViewRect.gameObject, "Content", true);

        GameObject prefab = Resources.Load<GameObject>("Prefabs/ParamBlock");
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : Prefabs/ParamBlock");
            return null;
        }
        GameObject paramBlockObj = GameObject.Instantiate(prefab);//, gameObject.transform.parent);

        GetButton((int)Buttons.AddParamBtn).gameObject.transform.SetParent(null);
        paramBlockObj.transform.SetParent(scrollViewContent.transform);
        GetButton((int)Buttons.AddParamBtn).gameObject.transform.SetParent(scrollViewContent.transform);
        Canvas.ForceUpdateCanvases();
        paramViewRect.verticalNormalizedPosition = 0;

        ParamBlock paramBlock = paramBlockObj.GetComponent<ParamBlock>();
        paramBlock.ParamBlockChangeHandler += OnParamBlockChanged;
        paramBlock.CancelHandler += OnParamBlockCanceled;

        ParamBlocks.Add(paramBlock);

        return paramBlock;
    }
}
