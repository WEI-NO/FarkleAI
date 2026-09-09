using UnityEngine;

public class GuideScreen : MonoBehaviour
{
    private Animator anim;

    [SerializeField] private bool Active;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        Active = true;
    }

    public void Toggle()
    {
        if (Active)
        {
            Hide();
        } else
        {
            Show();
        }

        Active = !Active;
    }
    
    public void Show()
    {
        anim.SetTrigger("Show");
    }

    public void Hide()
    {
        anim.SetTrigger("Hide");
    }
}
