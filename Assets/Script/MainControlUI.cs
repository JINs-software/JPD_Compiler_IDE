using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Object = UnityEngine.Object;


public class MainControlUI : UI_Base
{
    enum Buttons
    {
        NewBtn,
        LoadBtn,
        SaveBtn,
        CompileBtn,
        EditBtn,
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
        JsonPathInput,
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
        Button crtBtn = GetButton((int)Buttons.EditBtn);
        Button cancelBtn = GetButton((int)Buttons.CancelBtn);

        BindEvent(newBtn.gameObject, OnNewBtnClicked, Define.UIEvent.Click);
        BindEvent(loadBtn.gameObject, OnLoadBtnClicked, Define.UIEvent.Click);
        BindEvent(serverToggle.gameObject, OnServerToggleClicked, Define.UIEvent.Click);
        BindEvent(clientToggle.gameObject, OnClientToggleClicked, Define.UIEvent.Click);
        BindEvent(serverPathInput.gameObject, OnServerPathInputClicked, Define.UIEvent.Click);
        BindEvent(cliendPathInput.gameObject, OnClientInputClicked, Define.UIEvent.Click);
        BindEvent(crtBtn.gameObject, OnEditBtnClicked, Define.UIEvent.Click);
        BindEvent(cancelBtn.gameObject, OnCancelBtnClicked, Define.UIEvent.Click);
        BindEvent(saveBtn.gameObject, OnSaveJsonBtnClicked, Define.UIEvent.Click);  
        BindEvent(compileBtn.gameObject, OnCompileBtnClicked, Define.UIEvent.Click);

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
        GetInputField((int)InputFields.ServerPathInput).interactable = true;
        GetInputField((int)InputFields.ClientPathInput).interactable = true;
        GetButton((int)Buttons.EditBtn).interactable = true;  
        GetButton((int)Buttons.CancelBtn).interactable = true;  
    }

    // Click LoadBtn
    public void OnLoadBtnClicked(PointerEventData eventData)
    {
        Debug.Log("OnLoadBtnClicked");
        string jsonFilePath;
        if (OpenJsonFileExplorer(out jsonFilePath))
        {
            GetInputField((int)InputFields.JsonPathInput).text = jsonFilePath;
            JPDCompiler.Instance.LoadJson(jsonFilePath);

            // 컴파일 모드 설정
            if(JPDCompiler.JPDSchema.COMPILE_MODE == "RPC")
            {
                GetDropdown((int)Dropdowns.CompileMode).value = 0;
            }
            else if(JPDCompiler.JPDSchema.COMPILE_MODE == "HeaderOnly")
            {
                GetDropdown((int)Dropdowns.CompileMode).value = 1;
            }

            // 서버 경로 설정
            if(JPDCompiler.JPDSchema.SERVER_OUTPUT_DIR != string.Empty)
            {
                GetToggle((int)Toggles.ServerToggle).isOn = true;
            }
            else
            {
                GetToggle((int)Toggles.ServerToggle).isOn = false;
            }
            GetInputField((int)InputFields.ServerPathInput).text = JPDCompiler.JPDSchema.SERVER_OUTPUT_DIR;

            // 클라이언트 경로 설정
            if (JPDCompiler.JPDSchema.CLIENT_OUTPUT_DIR!= string.Empty)
            {
                GetToggle((int)Toggles.ClientToggle).isOn = true;
            }
            else
            {
                GetToggle((int)Toggles.ClientToggle).isOn = false;
            }
            GetInputField((int)InputFields.ClientPathInput).text = JPDCompiler.JPDSchema.CLIENT_OUTPUT_DIR;


            GetButton((int)Buttons.NewBtn).interactable = false;
            GetButton((int)Buttons.LoadBtn).interactable = false;
            GetDropdown((int)Dropdowns.CompileMode).interactable = true;
            GetToggle((int)Toggles.ServerToggle).interactable = true;
            GetToggle((int)Toggles.ClientToggle).interactable = true;
            GetInputField((int)InputFields.ServerPathInput).interactable = true;
            GetInputField((int)InputFields.ClientPathInput).interactable = true;
            GetButton((int)Buttons.EditBtn).interactable = true;
            GetButton((int)Buttons.CancelBtn).interactable = true;
        }
        else
        {
            GetInputField((int)InputFields.JsonPathInput).text = "Enter the correct file path";
        }
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

    // Click ServerPathInput
    public void OnServerPathInputClicked(PointerEventData eventdata)
    {
        string severDirPath;
        if (OpenDirectoryExplorer(out severDirPath))
        {
            JPDCompiler.JPDSchema.SERVER_OUTPUT_DIR = severDirPath;
            GetInputField((int)InputFields.ServerPathInput).text = severDirPath;    
        }
    }
    // Click ClientPathInput
    public void OnClientInputClicked(PointerEventData eventdata)
    {
        string clientDirPath;
        if (OpenDirectoryExplorer(out clientDirPath))
        {
            JPDCompiler.JPDSchema.SERVER_OUTPUT_DIR = clientDirPath;
            GetInputField((int)InputFields.ClientPathInput).text = clientDirPath;
        }
    }

    // Click EditBtn
    public void OnEditBtnClicked(PointerEventData eventData)
    {
        Debug.Log("OnEditBtnClicked");

        if (!GetToggle((int)Toggles.ServerToggle).isOn && !GetToggle((int)Toggles.ClientToggle).isOn)
        {
            Debug.Log("toggle not selected");
            return;
        }

        int compileMode = GetDropdown((int)Dropdowns.CompileMode).value;
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

        JPDCompiler.JPDSchema.COMPILE_MODE = Enum.GetName(typeof(CompileMode), (CompileMode)compileMode);
        JPDCompiler.JPDSchema.SERVER_OUTPUT_DIR = serverpath;
        JPDCompiler.JPDSchema.CLIENT_OUTPUT_DIR = clientpath;

        // 컴파일 모드에 따른 UI 추가
        if (compileMode == (int)CompileMode.RPC)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/JRpcGroup");
            if (prefab == null)
            {
                Debug.Log($"Failed to load prefab : Prefabs/JRpcGroup");
                return;
            }
            Object.Instantiate(prefab, gameObject.transform.parent);
        }
        else if(compileMode == (int)CompileMode.HEADER_ONLY)
        {

        }

        GetButton((int)Buttons.SaveBtn).interactable = true;
        GetButton((int)Buttons.CompileBtn).interactable = true;  
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
        GetButton((int)Buttons.EditBtn).interactable = false;
        GetButton((int)Buttons.CancelBtn).interactable = false;
    }

    // Click SaveJsonBtn
    public void OnSaveJsonBtnClicked(PointerEventData eventData)
    {
        Debug.Log("OnSaveJsonBtnClicked");

        string jsonPath = GetInputField((int)InputFields.JsonPathInput).text;
        JPDCompiler.Instance.SaveJsonFile(jsonPath);    
    }

    // Click CompileBtn
    public void OnCompileBtnClicked(PointerEventData eventdata)
    {
        Debug.Log("OnCompileBtnClicked");

        if (JPDCompiler.Instance.Complie())
        {
            Debug.Log("Compile Success!");
        }
        else
        {
            Debug.Log("Compile Fail..");
        }
    }

    // Open Window File Explorer
    public bool OpenJsonFileExplorer(out string seletecFilePath)
    {
       OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.InitialDirectory = Environment.CurrentDirectory;
        openFileDialog.Filter = "JSON files (*.json)|*.json";
        openFileDialog.FilterIndex = 1;
        openFileDialog.RestoreDirectory = true;

        if (openFileDialog.ShowDialog() != DialogResult.OK)
        {
            seletecFilePath = string.Empty;
            return false;
        }

        seletecFilePath = openFileDialog.FileName;
        Debug.Log("Selected file path: " + seletecFilePath);
        return true;
    }

    public bool OpenDirectoryExplorer(out string selectedDirectoryPath)
    {
        using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
        {
            folderBrowserDialog.Description = "Select a directory";
            folderBrowserDialog.SelectedPath = Environment.CurrentDirectory;

            if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            {
                selectedDirectoryPath = string.Empty;
                return false;
            }

            selectedDirectoryPath = folderBrowserDialog.SelectedPath;
            Debug.Log("Selected directory path: " + selectedDirectoryPath);
            return true;
        }
    }
}
