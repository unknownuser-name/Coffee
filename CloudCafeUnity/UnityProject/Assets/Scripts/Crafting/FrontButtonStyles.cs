using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private GUIStyle visibleActionStyle, disabledActionStyle;
        private Texture2D[] actionButtonTextures;

        private void EnsureButtonStyles()
        {
            if (visibleActionStyle != null) return;
            actionButtonTextures = new[]
            {
                CreateButtonTexture("Button gold", new Color32(242, 186, 104, 255), new Color32(255, 224, 167, 255)),
                CreateButtonTexture("Button hover", new Color32(255, 225, 174, 255), new Color32(255, 248, 229, 255)),
                CreateButtonTexture("Button pressed", new Color32(204, 135, 61, 255), new Color32(244, 188, 111, 255)),
                CreateButtonTexture("Button disabled", new Color32(157, 146, 129, 255), new Color32(194, 183, 164, 255))
            };
            Color ink = new Color32(43, 32, 24, 255);
            visibleActionStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(1, 1, 1, 1),
                padding = new RectOffset(6, 6, 3, 3),
                margin = new RectOffset(0, 0, 0, 0),
                fixedHeight = 0,
                fixedWidth = 0,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            SetButtonState(visibleActionStyle.normal, actionButtonTextures[0], ink);
            SetButtonState(visibleActionStyle.hover, actionButtonTextures[1], ink);
            SetButtonState(visibleActionStyle.active, actionButtonTextures[2], ink);
            SetButtonState(visibleActionStyle.focused, actionButtonTextures[1], ink);
            SetButtonState(visibleActionStyle.onNormal, actionButtonTextures[1], ink);
            SetButtonState(visibleActionStyle.onHover, actionButtonTextures[1], ink);
            SetButtonState(visibleActionStyle.onActive, actionButtonTextures[2], ink);
            SetButtonState(visibleActionStyle.onFocused, actionButtonTextures[1], ink);

            disabledActionStyle = new GUIStyle(visibleActionStyle);
            foreach (GUIStyleState state in new[]
            {
                disabledActionStyle.normal, disabledActionStyle.hover, disabledActionStyle.active,
                disabledActionStyle.focused, disabledActionStyle.onNormal, disabledActionStyle.onHover,
                disabledActionStyle.onActive, disabledActionStyle.onFocused
            })
                SetButtonState(state, actionButtonTextures[3], ink);
        }

        private static Texture2D CreateButtonTexture(string name, Color32 fill, Color32 border)
        {
            var texture = new Texture2D(3, 3, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(new[]
            {
                border, border, border,
                border, fill, border,
                border, border, border
            });
            texture.Apply(false, true);
            return texture;
        }

        private static void SetButtonState(GUIStyleState state, Texture2D background, Color text)
        {
            state.background = background;
            state.textColor = text;
        }

        private void DisposeButtonStyles()
        {
            if (actionButtonTextures != null)
                foreach (Texture2D texture in actionButtonTextures)
                    if (texture != null) Destroy(texture);
            actionButtonTextures = null;
            visibleActionStyle = disabledActionStyle = null;
        }
    }
}
