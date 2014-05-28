using UnityEngine;
using System.Collections;

public class ScalableGUI
{
    float _scale;
    Vector2 _offset;

    public ScalableGUI (int w = 320, int h = 480, bool isPortrait = false)
    {
        float width = isPortrait ? h : w;
        float height = isPortrait ? w : h;

        float target_aspect = width / height;
        float window_aspect = (float)Screen.width / (float)Screen.height;
        float scale = window_aspect / target_aspect;

        Rect _rect = new Rect (0.0f, 0.0f, 1.0f, 1.0f);
        if (1.0f > scale) {
            _rect.x = 0;
            _rect.width = 1.0f;
            _rect.y = (1.0f - scale) / 2.0f;
            _rect.height = scale;

            _scale = (float)Screen.width / width;
        } else {
            scale = 1.0f / scale;
            _rect.x = (1.0f - scale) / 2.0f;
            _rect.width = scale;
            _rect.y = 0.0f;
            _rect.height = 1.0f;

            _scale = (float)Screen.height / height;
        }

        _offset.x = _rect.x * Screen.width;
        _offset.y = _rect.y * Screen.height;
    }

    private Rect ScalableRect (float x, float y, float width, float height)
    { 
        Rect rect = new Rect (_offset.x + (x * _scale), _offset.y + (y * _scale), width * _scale, height * _scale);
        return rect;
    }

    public void Label (float x, float y, float width, float height, string text, float fontSize = 14)
    {
        GUIStyle style = new GUIStyle (GUI.skin.label);
        style.fontSize = (int)(fontSize * _scale);
        GUI.Label (ScalableRect (x, y, width, height), text, style);
    }

    public bool Button (float x, float y, float width, float height, string text, float fontSize = 14)
    {
        GUIStyle style = new GUIStyle (GUI.skin.button);
        style.fontSize = (int)(fontSize * _scale);
        return GUI.Button (ScalableRect (x, y, width, height), text, style);
    }

    public string TextField (float x, float y, float width, float height, string text, float fontSize = 14)
    {
        GUIStyle style = new GUIStyle (GUI.skin.textField);
        style.fontSize = (int)(fontSize * _scale);
        return GUI.TextField (ScalableRect (x, y, width, height), text, style);
    }

    public string TextArea (float x, float y, float width, float height, string text, float fontSize = 14)
    {
        GUIStyle style = new GUIStyle (GUI.skin.textField);
        style.fontSize = (int)(fontSize * _scale);
        return GUI.TextArea (ScalableRect (x, y, width, height), text, style);
    }

    public bool Toggle (float x, float y, float width, float height, bool value, string text, float fontSize = 14)
    {
        GUIStyle labelStyle = new GUIStyle (GUI.skin.label);
        labelStyle.fontSize = (int)(fontSize * _scale);
        labelStyle.alignment = TextAnchor.MiddleLeft;
        int labelLeftMargin = (int)(2 * _scale);
        GUI.Label (ScalableRect (x + height + labelLeftMargin, y, width - height - labelLeftMargin, height), text, labelStyle);

        string check = "";
        if (value) {
            check = "x";
        }
        GUIStyle style = new GUIStyle (GUI.skin.button);
        style.fontSize = (int)(height * _scale * 0.7);
        style.alignment = TextAnchor.MiddleCenter;
        int buttonMargin = (int)(2 * _scale);
        return GUI.Button (ScalableRect (x + buttonMargin, y + buttonMargin, height - (2 * buttonMargin), height - (2 * buttonMargin)), check, style);
    }
}

