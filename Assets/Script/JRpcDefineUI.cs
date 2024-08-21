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
    public JPD_DEFINE JpdDefine = null;
    Dictionary<ParamBlock, JPD_PARAM> Parameters = new Dictionary<ParamBlock, JPD_PARAM>();

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
        ScrollRect paramViewRect = Get<ScrollRect>((int)ScrollRects.PARAM_VIEW);
        GameObject scrollViewContent = Utill.FindChild(paramViewRect.gameObject, "Content", true);
        GameObject newParamBlock = CreateParamBlock();
        ParamBlock paramBlockComp = newParamBlock.GetComponent<ParamBlock>();
        BindEvent(paramBlockComp.OkBtn.gameObject, OnParamOkBtnClicked, Define.UIEvent.Click);
        BindEvent(paramBlockComp.DeleteBtn.gameObject, OnParamDeleteBtnClicked, Define.UIEvent.Click);        
        Parameters.Add(newParamBlock.GetComponent<ParamBlock>(), new JPD_PARAM());

        GetButton((int)Buttons.AddParamBtn).gameObject.transform.SetParent(null);
        newParamBlock.transform.SetParent(scrollViewContent.transform);
        GetButton((int)Buttons.AddParamBtn).gameObject.transform.SetParent(scrollViewContent.transform);

        Canvas.ForceUpdateCanvases();
        paramViewRect.verticalNormalizedPosition = 0;
    }

    public void OnParamOkBtnClicked(PointerEventData eventdata)
    {
        GameObject paramBlockOb = eventdata.selectedObject.transform.parent.gameObject;
        ParamBlock paramBlock = paramBlockOb.GetComponent<ParamBlock>();    
        JPD_PARAM jpdParam = Parameters[paramBlock];
        jpdParam.type = paramBlock.TypeInput.text;
        jpdParam.name = paramBlock.NameInput.text;

        SaveJpdDefine();
    }
    public void OnParamDeleteBtnClicked(PointerEventData eventdata)
    {
        GameObject paramBlockObj = eventdata.selectedObject.transform.parent.gameObject;
        ParamBlock paramBlock = paramBlockObj.GetComponent<ParamBlock>();   
        paramBlock.gameObject.transform.SetParent(null);
        Parameters.Remove(paramBlock);  
        GameObject.Destroy(paramBlock.gameObject);

        SaveJpdDefine();
    }

    private GameObject CreateParamBlock()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/ParamBlock");
        if (prefab == null)
        {
            Debug.Log($"Failed to load prefab : Prefabs/ParamBlock");
            return null;
        }
        GameObject paramBlock = Object.Instantiate(prefab);//, gameObject.transform.parent);

        return paramBlock;
    }

    private void SaveJpdDefine()
    {
        Text messageText = Utill.FindChild(JpdBlockBtn.gameObject, "MessageText").GetComponent<Text>();
        messageText.text = "";

        JpdDefine.Name = GetInputField((int)InputFields.NameInputField).text;
        if (GetToggle((int)Toggles.S2CToggle).isOn)
        {
            JpdDefine.Dir = "S2C";
        }
        else
        {
            JpdDefine.Dir = "C2S";
        }

        messageText.text = JpdDefine.Name + "_" + JpdDefine.Dir;
        messageText.text += "\r\n";
        messageText.text += "(";

        JpdDefine.Param.Clear();
        foreach (var param in Parameters)
        {
            JPD_PARAM jpdParam = param.Value;
            JpdDefine.Param.Add(jpdParam);

            messageText.text += jpdParam.type + " " + jpdParam.name + ", ";
        }
        messageText.text = messageText.text.Substring(0, messageText.text.Length - 2);
        messageText.text += ")";
    }
}
