using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MessageBlock : UI_Base
{
    public JPD_MESSAGE JpdMessage = null;

    private Text MessageTxt;
    private Button DelBtn;

    public Action<MessageBlock> OnClickHandler;
    public Action<MessageBlock> OnDeleteHandler;

    private void Start()
    {
        MessageTxt = Util.FindChild(gameObject, "MessageText").GetComponent<Text>();
        DelBtn = Util.FindChild(gameObject, "DelBtn").GetComponent<Button>();

        BindEvent(gameObject, OnClick);
        BindEvent(DelBtn.gameObject, OnDelBtnClicked);
    }

    public void OnClick(PointerEventData eventData)
    {
        OnClickHandler.Invoke(this);
    }

    public void OnDelBtnClicked(PointerEventData eventData)
    {
        OnDeleteHandler.Invoke(this);   
    }

    public void ResetBlockText()
    {
        if(MessageTxt == null)
        {
            MessageTxt = Util.FindChild(gameObject, "MessageText").GetComponent<Text>();
        }

        string msgTxt = $"{JpdMessage.Dir}_{JpdMessage.Message}";
        if (JpdMessage.Param.Count > 0)
        {
            msgTxt += "\n(";
            for (int i = 0; i < JpdMessage.Param.Count; i++)
            {
                if (string.IsNullOrEmpty(JpdMessage.Param[i].FixedLenOfArray))
                {
                    msgTxt += $"{JpdMessage.Param[i].Type} {JpdMessage.Param[i].Name}";
                }
                else
                {
                    msgTxt += $"{JpdMessage.Param[i].Type}[{JpdMessage.Param[i].FixedLenOfArray}] {JpdMessage.Param[i].Name}";
                }
                if (i == JpdMessage.Param.Count - 1)
                {
                    msgTxt += ")";
                }
                else
                {
                    msgTxt += ", ";
                }
            }
        }
        MessageTxt.text = msgTxt;   
    }
}
