using UnityEngine;

public interface IPlayerAnimator
{
    void PlayIdle();
    void PlayeEquippedOverride();
    void PlayEquipped();
    void PlayUnEquipped();

}
public class PlayerAnimationController : MonoBehaviour,IPlayerAnimator
{
    [Header("Ref")]
    private PlayerController playerController;
 
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void PlayIdle()
    {

    }
    public void PlayeEquippedOverride()
    {

    }
    public void PlayEquipped()
    {

    }
    public void PlayUnEquipped()
    {

    }

}
