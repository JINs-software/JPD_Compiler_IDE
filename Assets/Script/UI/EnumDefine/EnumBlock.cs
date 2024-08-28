using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnumBlock : MonoBehaviour
{
    public string EnumName;
    public InputField fieldInput;
    public Button cancelBtn;

    public Action<string> ValueChangedHndler;
    public Action<string, EnumBlock> CancelHandler;

    private void Start()
    {
        fieldInput.onEndEdit.AddListener(OnFieldEndEdit);
        cancelBtn.onClick.AddListener(OnCancelBtnClicked);
    }

    public void OnFieldEndEdit(string text)
    {
        ValueChangedHndler.Invoke(EnumName);
    }

    public void OnCancelBtnClicked()
    {
        CancelHandler.Invoke(EnumName, this);
    }
}
