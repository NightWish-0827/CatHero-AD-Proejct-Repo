using UnityEngine;
using TMPro; 
public class RichTextBinder : MonoBehaviour
{
    public TMP_Text text;

    void Start()
    {
        string raw =
            "적이 나타났습니다.\n" +
            "룰렛을 돌려 공격 수단을 뽑아보세요!";

        // 색 입히기
        raw = raw.Replace("적", "<color=red>적</color>");
        raw = raw.Replace("룰렛", "<color=yellow>룰렛</color>");

        text.text = raw;
    }
}
