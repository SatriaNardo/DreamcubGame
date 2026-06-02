using UnityEngine;

public class MenuCharacter : MonoBehaviour
{
    private Animator animator;

    public float attackInterval = 5f;

    private float timer;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= attackInterval)
        {
            animator.SetTrigger("Attack");
            timer = 0f;
        }
    }
}