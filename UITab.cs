using UnityEngine;
using System.Collections.Generic;
using XMLEngine.GameEngine.SilverLight;
using XMLEngine.GameFramework.Logic;

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Tab")]
public class UITab : MonoBehaviour
{
    public Transform myTransform;

    public List<GButton> tabBtns = null;
    public Color m_HotTextColor = Color.white;
    public Color m_HotEffectColor = Color.black;

    public Color m_NormalTextColor = Color.cyan;
    public Color m_NormalEffectColor = Color.white;

    public Color m_LockTextColor = new Color(0.35f, 0.35f, 0.37f, 1);
    public Color m_LockTextEffectColor = Color.black;

    
    
    
    public Color m_HotBakColor = Color.white;
    public Color m_NormalBakColor = Color.white;

    public bool m_bIsSetTextColor = false;
    
    public bool m_whetherCanNotBoxShowAndDis = false;

    public bool m_SetTextOutline = false;
    public delegate void TabClickHandler(GameObject sender, int index);
    public event TabClickHandler TabClick;
    
    
    
    public DPSelectedItemBoolEventHandler PreChangeTabCallback = null;
    public GameObject TweenNode = null;
    private TweenPosition _twPos = null;
    private TweenAlpha _twAlpha = null;

    void Awake()
    {
        if (null == myTransform)
        {
            myTransform = transform;
        }
        if (tabBtns == null || tabBtns.Count == 0)
            tabBtns = NGUITools.GetComponentList<GButton>(myTransform);
        InitEvent();
        if (TweenNode)
        {
            _twAlpha = TweenNode.GetComponent<TweenAlpha>();
            _twPos = TweenNode.GetComponent<TweenPosition>();
        }
    }

    private int _TabIndex = -1;
    public int TabIndex
    {
        get { return _TabIndex; }
        set { SetTab(value); }
    }

    public bool SetBoxShowAndDis
    {
        get { return m_whetherCanNotBoxShowAndDis; }
        set { m_whetherCanNotBoxShowAndDis = value; }
    }

    public GButton this[int i]
    {
        get
        {
            foreach (GButton obj in tabBtns)
            {
                if (obj.TagIndex == i)
                {
                    return obj;
                }
            }
            return null;

        }
    }

    void SetTabBtn(List<GButton> list, int index)
    {
        if (null != list)
        {
            foreach (GButton obj in list)
            {
                if (obj.TagIndex == index)
                {
                    _TabIndex = index;
                    obj.Pressed = true;
                    if (!m_whetherCanNotBoxShowAndDis)
                    {
                        obj.isEnabled = false;
                    }
                    if (m_bIsSetTextColor)
                    {
                        UILabel lblText = obj.gameObject.GetComponentInChildren<UILabel>();
                        if (null != lblText)
                        {
                            lblText.color = m_HotTextColor;
                            lblText.effectColor = m_HotEffectColor;
                            if (m_SetTextOutline)
                                lblText.effectStyle = UILabel.Effect.Outline;
                        }
                        if (obj.target != null)
                        {
                            obj.target.color = m_HotBakColor;
                        }
                    }
                    var anim = obj.GetComponent<Animation>();
                    if (anim != null && anim.clip != null)
                    {
                        anim.Play();
                    }
                }
                else
                {
                    if (m_whetherCanNotBoxShowAndDis == false)
                    {
                        obj.Pressed = false;
                        obj.isEnabled = true;
                    }
                    if (m_bIsSetTextColor)
                    {
                        UILabel lblText = obj.gameObject.GetComponentInChildren<UILabel>();
                        if (null != lblText)
                        {
                            lblText.color = m_NormalTextColor;
                            lblText.effectColor = m_NormalEffectColor;
                            if (m_SetTextOutline)
                                lblText.effectStyle = UILabel.Effect.None;
                        }
                        if (obj.target != null)
                        {
                            obj.target.color = m_NormalBakColor;
                        }
                    }
                }
                if (m_whetherCanNotBoxShowAndDis == false)
                {
                    obj.Refresh();
                }
            }
        }
    }

    void InitEvent()
    {
        if (null != tabBtns)
        {
            foreach (GButton obj in tabBtns)
            {
                obj.MouseLeftButtonUp = SetTab;
            }
        }
    }

    

    void SetTab(int index)
    {
        SetTabBtn(tabBtns, index);

        if (null != TabClick)
        {
            TabClick(null, index);
        }
    }

    void SetTab(object sender, MouseEvent e)
    {
        GButton btn = (sender as GameObject).GetComponent<GButton>();
        if (null == btn)
        {
            XMLDebug.Log("btn为null");
            return;
        }
        int index = btn.TagIndex;
        if (null != PreChangeTabCallback)
        {
            if (!PreChangeTabCallback(sender, new DPSelectedItemEventArgs { ID = index }))
            {
                return;
            }
        }

        SetTabBtn(tabBtns, index);

        if (null != TabClick)
        {
            TabClick(null, index);
        }
        if (TweenNode)
        {
            if (_twAlpha)
            {
                _twAlpha.enabled = true;
                _twAlpha.ResetToBeginning();
            }
            if (_twPos)
            {
                _twPos.enabled = true;
                _twPos.ResetToBeginning();
            }
        }
    }
    private void OnDestroy()
    {
        TabClick = null;
        PreChangeTabCallback = null;
        if (tabBtns != null)
            tabBtns.Clear();
    }
}
