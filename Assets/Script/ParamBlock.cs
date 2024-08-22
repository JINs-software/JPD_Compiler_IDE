using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ParamBlock : MonoBehaviour
{
    public JPD_PARAM JpdParam;
    public InputField TypeInput;
    public InputField NameInput;
    public Button OkBtn;
    public Button DeleteBtn;

    public void Reset()
    {
        JpdParam.Type = TypeInput.text;
        JpdParam.Name = NameInput.text; 
    }
}
