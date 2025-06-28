using UnityEngine;
using DG.Tweening;

public class HomeCanvasAnimator : MonoBehaviour
{
    public RectTransform logo;
    public float logoDropY = 0f;
    public float logoDropDuration = 0.8f;

    public CanvasGroup[] buttons; // các button có CanvasGroup
    public float buttonFadeDelay = 0.2f;
    public float buttonFadeDuration = 0.4f;

    private Vector2 logoStartPos;

    void OnEnable()
    {
        AnimateIntro();
    }

    public void AnimateIntro()
    {
        if (logo == null) return;

        // Lưu lại vị trí đích của logo
        logoStartPos = logo.anchoredPosition;

        // Di chuyển logo lên cao trước khi thả xuống
        logo.anchoredPosition = new Vector2(logoStartPos.x, logoStartPos.y + 500);

        // Logo rơi xuống
        logo.DOAnchorPosY(logoStartPos.y, logoDropDuration)
            .SetEase(Ease.OutBounce);

        // Fade từng button
        for (int i = 0; i < buttons.Length; i++)
        {
            CanvasGroup btn = buttons[i];
            btn.alpha = 0f;
            btn.gameObject.SetActive(true);

            btn.DOFade(1f, buttonFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetDelay(logoDropDuration + i * buttonFadeDelay);
        }
    }
}
