using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainControlUI : UI_Base
{
    enum Buttons
    {
        NewBtn,
        LoadBtn,
        SaveBtn,
        CompileBtn,
        CreateBtn,
        CancelBtn,
    }

    enum Toggles
    {
        ServerToggle,
        ClientToggle,
    }

    enum Dropdowns
    {
        CompileMode,
    }

    enum InputFields
    {
        ServerPathInput,
        ClientPathInput,
    }

    enum CompileMode
    {
        RPC,
        HEADER_ONLY,
    }

    private void Start()
    {
        Bind<Button>(typeof(Buttons));
        Bind<Toggle>(typeof(Toggles));
        Bind<Dropdown>(typeof(Dropdowns));
        Bind<InputField>(typeof(InputFields));   

        Button newBtn = GetButton((int)Buttons.NewBtn);
        Button loadBtn = GetButton((int)Buttons.LoadBtn);
        Button saveBtn = GetButton((int)Buttons.SaveBtn);
        Button compileBtn = GetButton((int)Buttons.CompileBtn);
        Dropdown complieModeDropDown = GetDropdown((int)Dropdowns.CompileMode);
        Toggle serverToggle = GetToggle((int)Toggles.ServerToggle);
        Toggle clientToggle = GetToggle((int)Toggles.ClientToggle);
        InputField serverPathInput = GetInputField((int)InputFields.ServerPathInput);
        InputField cliendPathInput = GetInputField((int)InputFields.ClientPathInput);
        Button crtBtn = GetButton((int)Buttons.CreateBtn);
        Button cancelBtn = GetButton((int)Buttons.CancelBtn);

        BindEvent(newBtn.gameObject, OnNewBtnClicked, Define.UIEvent.Click);
        BindEvent(loadBtn.gameObject, OnLoadBtnClicked, Define.UIEvent.Click);
        BindEvent(serverToggle.gameObject, OnServerToggleClicked, Define.UIEvent.Click);
        BindEvent(clientToggle.gameObject, OnClientToggleClicked, Define.UIEvent.Click);
        BindEvent(crtBtn.gameObject, OnCreateBtnClicked, Define.UIEvent.Click);
        BindEvent(cancelBtn.gameObject, OnCancelBtnClicked, Define.UIEvent.Click);

        // 초기 화면
        newBtn.interactable = true; 
        loadBtn.interactable = true;
        saveBtn.interactable = false;
        compileBtn.interactable = false;

        complieModeDropDown.interactable = false;
        serverToggle.interactable = false;
        clientToggle.interactable = false;  
        serverPathInput.interactable = false;
        cliendPathInput.interactable = false;
        crtBtn.interactable = false;
        cancelBtn.interactable = false; 
    }

    // Click NewBtn
    public void OnNewBtnClicked(PointerEventData eventData)
    {
        Debug.Log("OnNewBtnClicked");
        GetButton((int)Buttons.NewBtn).interactable = false;
        GetButton((int)Buttons.LoadBtn).interactable = false;

        GetDropdown((int)Dropdowns.CompileMode).interactable = true;
        GetToggle((int)Toggles.ServerToggle).interactable = true;
        GetToggle((int)Toggles.ClientToggle).interactable = true;
        GetButton((int)Buttons.CreateBtn).interactable = true;  
        GetButton((int)Buttons.CancelBtn).interactable = true;  
    }

    // Click LoadBtn
    public void OnLoadBtnClicked(PointerEventData eventData)
    {
        Debug.Log("OnLoadBtnClicked");
    }

    // Set Server Toggle
    // Unset ""
    public void OnServerToggleClicked(PointerEventData eventData)
    {
        Debug.Log("OnServerToggleClicked");
        if (GetToggle((int)Toggles.ServerToggle).isOn)
        {
            GetInputField((int)InputFields.ServerPathInput).interactable = true;
        }
        else
        {
            GetInputField((int)InputFields.ServerPathInput).interactable = false;
        }
    }

    // Set Client Toggle
    // Unset ""
    public void OnClientToggleClicked(PointerEventData eventData)
    {
        Debug.Log("OnClientToggleClicked");
        if (GetToggle((int)Toggles.ClientToggle).isOn)
        {
            GetInputField((int)InputFields.ClientPathInput).interactable = true;
        }
        else
        {
            GetInputField((int)InputFields.ClientPathInput).interactable = false;
        }
    }

    // Click CreateBtn
    public void OnCreateBtnClicked(PointerEventData eventData)
    {
        Debug.Log("OnCreateBtnClicked");

        if (!GetToggle((int)Toggles.ServerToggle).isOn && !GetToggle((int)Toggles.ClientToggle).isOn)
        {
            Debug.Log("toggle not selected");
            return;
        }

        int compileMode = GetDropdown((int)Dropdowns.CompileMode).value;
        //Debug.Log(GetDropdown((int)Dropdowns.CompileMode).captionText);
        //Debug.Log(GetDropdown((int)Dropdowns.CompileMode).itemText);

        string serverpath = string.Empty;
        string clientpath = string.Empty;
        if (GetToggle((int)Toggles.ServerToggle).isOn)
        {
            serverpath = GetInputField((int)InputFields.ServerPathInput).text;
        }
        if (GetToggle((int)Toggles.ClientToggle).isOn)
        {
            clientpath = GetInputField((int)InputFields.ClientPathInput).text;
        }

        Debug.Log(compileMode);
        Debug.Log(serverpath);
        Debug.Log(clientpath);

        // 경로 체크

        // 컴파일 모드에 따른 UI 추가
        if(compileMode == (int)CompileMode.RPC)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/JRpcGroup");
            if (prefab == null)
            {
                Debug.Log($"Failed to load prefab : Prefabs/JRpcGroup");
                return;
            }
            Object.Instantiate(prefab, gameObject.transform.parent);

            //prefab = Resources.Load<GameObject>("Prefabs/JRpcDefGroup");
            //if (prefab == null)
            //{
            //    Debug.Log($"Failed to load prefab : Prefabs/JRpcDefGroup");
            //    return;
            //}
            //Object.Instantiate(prefab, gameObject.transform.parent);
        }
        else if(compileMode == (int)CompileMode.HEADER_ONLY)
        {

        }
    }

    // Click CancelBtn
    public void OnCancelBtnClicked(PointerEventData eventData)
    {
        Debug.Log("OnCancelBtnClicked");
        GetButton((int)Buttons.NewBtn).interactable = true;
        GetButton((int)Buttons.LoadBtn).interactable = true;

        GetDropdown((int)Dropdowns.CompileMode).interactable = false;
        GetToggle((int)Toggles.ServerToggle).isOn = false;
        GetToggle((int)Toggles.ClientToggle).isOn = false;
        GetToggle((int)Toggles.ServerToggle).interactable = false;
        GetToggle((int)Toggles.ClientToggle).interactable = false;
        GetInputField((int)InputFields.ServerPathInput).interactable = false;
        GetInputField((int)InputFields.ClientPathInput).interactable = false;
        GetButton((int)Buttons.CreateBtn).interactable = false;
        GetButton((int)Buttons.CancelBtn).interactable = false;
    }

    // Click SaveJsonBtn

    // Click CompileBtn
}
