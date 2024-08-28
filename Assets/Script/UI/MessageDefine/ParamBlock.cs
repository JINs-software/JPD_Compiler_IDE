using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ParamBlock : MonoBehaviour
{
    public InputField TypeInput;
    public Toggle ArrayToggle;
    public InputField FixedLengthInput;
    public InputField NameInput;
    public Button CancelBtn;

    public Action ParamBlockChangeHandler;
    public Action<ParamBlock> CancelHandler;

    private void Start()
    {
        TypeInput.onEndEdit.AddListener(OnTypeEndEdit);
        ArrayToggle.onValueChanged.AddListener(OnArrayToggleChanged);
        FixedLengthInput.onEndEdit.AddListener(OnFixedLengthEndEdit);
        NameInput.onEndEdit.AddListener(OnFixedLengthEndEdit);
        CancelBtn.onClick.AddListener(OnCancelBtnClicked);
    }

    public void OnTypeEndEdit(string text)
    {
        ParamBlockChangeHandler.Invoke();
    }
    public void OnArrayToggleChanged(bool on)
    {
        if (on)
        {
            FixedLengthInput.interactable = true;
        }
        else
        {
            FixedLengthInput.text = "";
            FixedLengthInput.interactable = false;  
        }
    }
    public void OnFixedLengthEndEdit(string text)
    {
        ParamBlockChangeHandler.Invoke();
    }
    public void OnNameEndEdit(string text)
    {
        ParamBlockChangeHandler.Invoke();
    }
    public void OnCancelBtnClicked()
    {
        CancelHandler.Invoke(this);
    }

}
