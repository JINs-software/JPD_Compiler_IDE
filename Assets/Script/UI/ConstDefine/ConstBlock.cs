using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConstBlock : MonoBehaviour
{
    public string ConstGroup;
    public InputField typeInput;
    public InputField nameInput;
    public InputField valueInput;
    public Button cancelBtn;

    public Action<string> ConstBlockChangeHandler;
    public Action<string, ConstBlock> CancelHandler;

    private void Start()
    {
        typeInput.onEndEdit.AddListener(OnTypeEndEdit);
        nameInput.onEndEdit.AddListener(OnNameEndEdit);
        valueInput.onEndEdit.AddListener(OnValueEndEdit);
        cancelBtn.onClick.AddListener(OnCancelBtnClicked);
    }

    public void OnTypeEndEdit(string text)
    {
        ConstBlockChangeHandler.Invoke(ConstGroup);
    }
    public void OnNameEndEdit(string text)
    {
        ConstBlockChangeHandler.Invoke(ConstGroup);
    }
    public void OnValueEndEdit(string text)
    {
        ConstBlockChangeHandler.Invoke(ConstGroup);
    }

    public void OnCancelBtnClicked()
    {
        CancelHandler.Invoke(ConstGroup, this);
    }
}
