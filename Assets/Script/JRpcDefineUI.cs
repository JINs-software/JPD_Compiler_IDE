using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JRpcDefineUI : UI_Base
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

    public Button JpdBlockBtn = null;
    public JPD_MESSAGE JpdMessage = null;
    //Dictionary<ParamBlock, JPD_PARAM> Parameters = new Dictionary<ParamBlock, JPD_PARAM>();

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

        GetInputField((int)InputFields.NameInputField).text = JpdMessage.Message;
        if(JpdMessage.Dir == "S2C")
        {
            s2cToggle.isOn = true;  
            c2sToggle.isOn = false;
        }
        else
        {
            s2cToggle.isOn = false;
            c2sToggle.isOn = true;
        }

        SetParamsInView();
    }

    public void OnS2CToggleClicked(PointerEventData eventdata)
    {
        if (GetToggle((int)Toggles.S2CToggle).isOn)
        {
            GetToggle((int)Toggles.C2SToggle).isOn = false; 
        }
    }

    public void OnC2SToggleClicked(PointerEventData eventdata)
    {
        if (GetToggle((int)Toggles.C2SToggle).isOn)
        {
            GetToggle((int)Toggles.S2CToggle).isOn = false;
        }
    }

    public void OnAddParamBtnClicked(PointerEventData eventdata)
    {
        JPD_PARAM newParam = new JPD_PARAM();
        JpdMessage.Param.Add(newParam);     
        AddParamBlock(newParam);    
    }

    public void OnParamOkBtnClicked(PointerEventData eventdata)
    {
        GameObject paramBlockOb = eventdata.selectedObject.transform.parent.gameObject;
        ParamBlock paramBlock = paramBlockOb.GetComponent<ParamBlock>();
        paramBlock.Reset();

        JRpcUI.SetJpdMessageBtntext(JpdBlockBtn.gameObject, JpdMessage);
    }
    public void OnParamDeleteBtnClicked(PointerEventData eventdata)
    {
        GameObject paramBlockObj = eventdata.selectedObject.transform.parent.gameObject;
        ParamBlock paramBlock = paramBlockObj.GetComponent<ParamBlock>();

        JpdMessage.Param.Remove(paramBlock.JpdParam);
        paramBlock.gameObject.transform.SetParent(null);
        GameObject.Destroy(paramBlockObj);

        JRpcUI.SetJpdMessageBtntext(JpdBlockBtn.gameObject, JpdMessage);
    }

    private void AddParamBlock(JPD_PARAM param)
    {
        ScrollRect paramViewRect = Get<ScrollRect>((int)ScrollRects.PARAM_VIEW);
        GameObject scrollViewContent = Utill.FindChild(paramViewRect.gameObject, "Content", true);
        GameObject newParamBlock = CreateParamBlock();
        ParamBlock paramBlockComp = newParamBlock.GetComponent<ParamBlock>();
        paramBlockComp.JpdParam = param;

        BindEvent(paramBlockComp.OkBtn.gameObject, OnParamOkBtnClicked, Define.UIEvent.Click);
        BindEvent(paramBlockComp.DeleteBtn.gameObject, OnParamDeleteBtnClicked, Define.UIEvent.Click);
        //Parameters.Add(newParamBlock.GetComponent<ParamBlock>(), new JPD_PARAM());

        GetButton((int)Buttons.AddParamBtn).gameObject.transform.SetParent(null);
        newParamBlock.transform.SetParent(scrollViewContent.transform);
        GetButton((int)Buttons.AddParamBtn).gameObject.transform.SetParent(scrollViewContent.transform);

        Canvas.ForceUpdateCanvases();
        paramViewRect.verticalNormalizedPosition = 0;

        paramBlockComp.TypeInput.text = param.Type;
        paramBlockComp.NameInput.text = param.Name;

        JRpcUI.SetJpdMessageBtntext(JpdBlockBtn.gameObject, JpdMessage);
    }

    private GameObject CreateParamBlock()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/ParamBlock");
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : Prefabs/ParamBlock");
            return null;
        }
        GameObject paramBlock = GameObject.Instantiate(prefab);//, gameObject.transform.parent);

        return paramBlock;
    }

    private void SetParamsInView()
    {
        foreach(var param in JpdMessage.Param)
        {
            AddParamBlock(param);
        }
    }

    /*
    private void SaveJpdDefine()
    {
        Text messageText = Utill.FindChild(JpdBlockBtn.gameObject, "MessageText").GetComponent<Text>();
        messageText.text = "";

        JpdMessage.Message = GetInputField((int)InputFields.NameInputField).text;
        if (GetToggle((int)Toggles.S2CToggle).isOn)
        {
            JpdMessage.Dir = "S2C";
        }
        else
        {
            JpdMessage.Dir = "C2S";
        }

        messageText.text = JpdMessage.Message + "_" + JpdMessage.Dir;
        messageText.text += "\r\n";
        messageText.text += "(";

        JpdMessage.Param.Clear();
        foreach (var param in Parameters)
        {
            JPD_PARAM jpdParam = param.Value;
            JpdMessage.Param.Add(jpdParam);

            messageText.text += jpdParam.Type + " " + jpdParam.Name + ", ";
        }
        messageText.text = messageText.text.Substring(0, messageText.text.Length - 2);
        messageText.text += ")";
    }
    */
}
