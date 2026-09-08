using UnityEngine;

public class ActionSwitcher : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))      // 按 1 = 走路（每次按都从头播）
            animator.Play("Walking", 0, 0f);
        else if (Input.GetKeyDown(KeyCode.Alpha2))  // 按 2 = 射击
            animator.Play("Shooting", 0, 0f);
        else if (Input.GetKeyDown(KeyCode.Alpha3))  // 按 3 = 快跑
            animator.Play("Fast Run", 0, 0f);
    }
}
