using UnityEngine;

namespace CloudCafe.Crafting
{
    public sealed partial class FrontGrindController
    {
        private static readonly Color PortraitInk = new Color32(48, 43, 43, 255);
        private static readonly Color PortraitPaper = new Color32(244, 232, 209, 255);
        private static readonly Color[] PortraitSkin =
        {
            new Color32(194, 133, 95, 255), new Color32(240, 190, 161, 255),
            new Color32(120, 77, 58, 255), new Color32(209, 151, 110, 255),
            new Color32(163, 103, 77, 255), new Color32(216, 178, 139, 255)
        };
        private static readonly Color[] PortraitHair =
        {
            new Color32(56, 44, 42, 255), new Color32(110, 57, 43, 255),
            new Color32(215, 215, 198, 255), new Color32(48, 43, 42, 255),
            new Color32(64, 44, 38, 255), new Color32(223, 223, 212, 255)
        };
        private static readonly Color[] PortraitClothes =
        {
            new Color32(187, 129, 60, 255), new Color32(65, 120, 104, 255),
            new Color32(157, 76, 78, 255), new Color32(94, 148, 143, 255),
            new Color32(138, 120, 166, 255), new Color32(70, 101, 128, 255)
        };

        // Original counter portraits are drawn in a fixed coordinate space, then fitted
        // into the requested UI rectangle. No imported character artwork is required.
        private void DrawCafeCustomerPortrait(Rect rect, int customerIndex, int expression)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            int index = ((customerIndex % PortraitSkin.Length) + PortraitSkin.Length) % PortraitSkin.Length;
            float scale = Mathf.Min(rect.width / 160f, rect.height / 190f);
            Vector3 origin = new Vector3(rect.x + (rect.width - 160f * scale) / 2f,
                rect.y + (rect.height - 190f * scale) / 2f);
            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.matrix = previous * Matrix4x4.TRS(origin, Quaternion.identity, new Vector3(scale, scale, 1f));
            try
            {
                PortraitRound(new Rect(0, 0, 160, 190), new Color32(39, 54, 59, 255), 15);
                PortraitRound(new Rect(5, 5, 150, 180), PortraitPaper, 12);
                PortraitRound(new Rect(14, 15, 132, 154), new Color32(225, 211, 185, 255), 65);
                DrawPortraitBackHair(index);
                PortraitRound(new Rect(26, 127, 108, 57), PortraitInk, 30);
                PortraitRound(new Rect(29, 130, 102, 54), PortraitClothes[index], 28);
                PortraitRound(new Rect(66, 112, 28, 31), PortraitInk, 11);
                PortraitRound(new Rect(69, 111, 22, 29), PortraitSkin[index] * new Color(0.90f, 0.90f, 0.90f, 1), 9);
                DrawPortraitClothing(index);
                PortraitRound(new Rect(40, 66, 16, 24), PortraitInk, 8);
                PortraitRound(new Rect(104, 66, 16, 24), PortraitInk, 8);
                PortraitRound(new Rect(43, 69, 12, 18), PortraitSkin[index], 7);
                PortraitRound(new Rect(105, 69, 12, 18), PortraitSkin[index], 7);
                PortraitRound(new Rect(46, 31, 68, 96), PortraitInk, 34);
                PortraitRound(new Rect(49, 34, 62, 90), PortraitSkin[index], 31);
                DrawPortraitFrontHair(index);
                DrawPortraitFace(index, expression);
                DrawPortraitAccessories(index);
            }
            finally
            {
                GUI.matrix = previous;
                GUI.color = previousColor;
            }
        }

        private static void DrawPortraitBackHair(int index)
        {
            Color hair = PortraitHair[index];
            switch (index)
            {
                case 1:
                    PortraitRound(new Rect(36, 24, 88, 111), PortraitInk, 38);
                    PortraitRound(new Rect(39, 27, 82, 105), hair, 35);
                    break;
                case 2:
                    PortraitRound(new Rect(35, 31, 26, 48), hair, 13);
                    PortraitRound(new Rect(99, 31, 26, 48), hair, 13);
                    break;
                case 3:
                    PortraitRound(new Rect(88, 12, 34, 32), PortraitInk, 17);
                    PortraitRound(new Rect(91, 15, 28, 26), hair, 14);
                    break;
                case 4:
                    PortraitRound(new Rect(37, 43, 15, 58), PortraitInk, 7);
                    PortraitRound(new Rect(108, 43, 15, 58), PortraitInk, 7);
                    break;
            }
        }

        private static void DrawPortraitFrontHair(int index)
        {
            Color hair = PortraitHair[index];
            switch (index)
            {
                case 0:
                    PortraitRound(new Rect(42, 27, 76, 39), new Color32(46, 71, 89, 255), 24);
                    PortraitRound(new Rect(40, 50, 80, 15), new Color32(60, 91, 111, 255), 5);
                    PortraitRound(new Rect(50, 57, 9, 17), hair, 4);
                    break;
                case 1:
                    PortraitRound(new Rect(42, 27, 76, 32), hair, 20);
                    PortraitLine(new Vector2(88, 31), new Vector2(48, 69), hair, 17);
                    PortraitLine(new Vector2(104, 37), new Vector2(115, 82), hair, 12);
                    break;
                case 2:
                    PortraitRound(new Rect(41, 28, 28, 25), hair, 13);
                    PortraitRound(new Rect(61, 21, 29, 28), hair, 14);
                    PortraitRound(new Rect(84, 23, 30, 28), hair, 15);
                    break;
                case 3:
                    PortraitRound(new Rect(45, 27, 70, 31), hair, 22);
                    PortraitLine(new Vector2(98, 34), new Vector2(49, 62), hair, 12);
                    PortraitRound(new Rect(46, 44, 9, 28), hair, 4);
                    break;
                case 4:
                    for (int i = 0; i < 5; i++)
                        PortraitRound(new Rect(40 + i * 15, 22 + (i % 2) * 5, 25, 29), hair, 13);
                    PortraitLine(new Vector2(41, 47), new Vector2(47, 68), hair, 7);
                    break;
                default:
                    PortraitRound(new Rect(42, 27, 75, 30), hair, 21);
                    PortraitLine(new Vector2(101, 32), new Vector2(50, 60), hair, 12);
                    PortraitRound(new Rect(44, 45, 10, 30), hair, 5);
                    PortraitRound(new Rect(106, 45, 10, 30), hair, 5);
                    break;
            }
        }

        private static void DrawPortraitClothing(int index)
        {
            Color cream = new Color32(246, 234, 211, 255);
            if (index == 1)
            {
                PortraitLine(new Vector2(56, 132), new Vector2(104, 139), new Color32(218, 163, 74, 255), 15);
                PortraitLine(new Vector2(94, 140), new Vector2(102, 165), new Color32(218, 163, 74, 255), 14);
            }
            else if (index == 3)
            {
                PortraitLine(new Vector2(55, 135), new Vector2(58, 180), cream, 8);
                PortraitLine(new Vector2(105, 135), new Vector2(102, 180), cream, 8);
                PortraitRound(new Rect(67, 156, 27, 22), new Color32(69, 119, 115, 255), 3);
            }
            else if (index == 0 || index == 5)
            {
                PortraitLine(new Vector2(60, 132), new Vector2(78, 150), cream, 10);
                PortraitLine(new Vector2(100, 132), new Vector2(82, 150), cream, 10);
                PortraitLine(new Vector2(80, 150), new Vector2(80, 182), PortraitInk, 2);
            }
            else
            {
                PortraitRound(new Rect(60, 128, 40, 15), PortraitInk, 7);
                PortraitRound(new Rect(64, 128, 32, 10), cream, 5);
            }
        }

        private static void DrawPortraitFace(int index, int expression)
        {
            if (expression > 0)
            {
                PortraitLine(new Vector2(59, 78), new Vector2(65, 74), PortraitInk, 2.5f);
                PortraitLine(new Vector2(65, 74), new Vector2(71, 78), PortraitInk, 2.5f);
                PortraitLine(new Vector2(89, 78), new Vector2(95, 74), PortraitInk, 2.5f);
                PortraitLine(new Vector2(95, 74), new Vector2(101, 78), PortraitInk, 2.5f);
            }
            else
            {
                PortraitRound(new Rect(62, 75, 5, 7), PortraitInk, 2.5f);
                PortraitRound(new Rect(92, 75, 5, 7), PortraitInk, 2.5f);
            }
            float browSlope = expression < 0 ? -4f : 1f;
            PortraitLine(new Vector2(58, 68), new Vector2(72, 68 + browSlope), PortraitHair[index], 3);
            PortraitLine(new Vector2(88, 68 + browSlope), new Vector2(102, 68), PortraitHair[index], 3);
            PortraitLine(new Vector2(80, 81), new Vector2(77, 91), PortraitSkin[index] * new Color(.72f, .72f, .72f, 1), 2);
            PortraitLine(new Vector2(77, 91), new Vector2(82, 92), PortraitSkin[index] * new Color(.72f, .72f, .72f, 1), 2);
            float mouthCenter = expression > 0 ? 106f : expression < 0 ? 100f : 104f;
            PortraitLine(new Vector2(69, 102), new Vector2(75, mouthCenter), PortraitInk, 2.5f);
            PortraitLine(new Vector2(75, mouthCenter), new Vector2(85, mouthCenter), PortraitInk, 2.5f);
            PortraitLine(new Vector2(85, mouthCenter), new Vector2(91, 102), PortraitInk, 2.5f);
        }

        private static void DrawPortraitAccessories(int index)
        {
            if (index == 2)
            {
                PortraitRound(new Rect(53, 66, 25, 23), PortraitInk, 11);
                PortraitRound(new Rect(56, 69, 19, 17), PortraitSkin[index], 9);
                PortraitRound(new Rect(83, 66, 25, 23), PortraitInk, 11);
                PortraitRound(new Rect(86, 69, 19, 17), PortraitSkin[index], 9);
                PortraitLine(new Vector2(77, 75), new Vector2(84, 75), PortraitInk, 2);
                PortraitRound(new Rect(63, 75, 4, 6), PortraitInk, 2);
                PortraitRound(new Rect(93, 75, 4, 6), PortraitInk, 2);
            }
            else if (index == 4)
            {
                Color headphones = new Color32(53, 102, 111, 255);
                PortraitRound(new Rect(34, 63, 12, 27), headphones, 5);
                PortraitRound(new Rect(114, 63, 12, 27), headphones, 5);
                PortraitLine(new Vector2(41, 61), new Vector2(44, 45), headphones, 4);
                PortraitLine(new Vector2(116, 61), new Vector2(113, 45), headphones, 4);
            }
            else if (index == 5)
            {
                PortraitRound(new Rect(68, 97, 12, 6), PortraitHair[index], 3);
                PortraitRound(new Rect(80, 97, 12, 6), PortraitHair[index], 3);
            }
            else if (index == 1 || index == 3)
            {
                Color jewelry = new Color32(226, 181, 87, 255);
                PortraitRound(new Rect(42, 86, 8, 10), jewelry, 4);
                PortraitRound(new Rect(110, 86, 8, 10), jewelry, 4);
            }
        }

        private static void PortraitRound(Rect rect, Color color, float radius)
        {
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radius);
        }

        private static void PortraitLine(Vector2 from, Vector2 to, Color color, float width)
        {
            Matrix4x4 previous = GUI.matrix;
            float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, from);
            PortraitRound(new Rect(from.x, from.y - width / 2f, Vector2.Distance(from, to), width), color, width / 2f);
            GUI.matrix = previous;
        }
    }
}
