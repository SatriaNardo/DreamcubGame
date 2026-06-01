using UnityEngine;

public class AbilityUnlocker : MonoBehaviour
{
    // This creates a nice dropdown menu in the Unity Inspector!
    public enum AbilityType { Jump, Dash, Attack }

    [Header("Power-Up Settings")]
    [Tooltip("Select which ability this item unlocks.")]
    public AbilityType abilityToUnlock;

    [Header("Visual Effects")]
    [Tooltip("Optional: Drag a particle system prefab here to spawn when collected.")]
    [SerializeField] private GameObject collectEffectPrefab;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Check if the player touched the item
        if (collision.CompareTag("Player"))
        {
            // 2. Grab the PlayerController script
            PlayerController player = collision.GetComponent<PlayerController>();

            if (player != null)
            {
                // 3. Grant the correct ability based on the dropdown menu
                switch (abilityToUnlock)
                {
                    case AbilityType.Jump:
                        player.UnlockJump();
                        break;
                    case AbilityType.Dash:
                        player.UnlockDash();
                        break;
                    case AbilityType.Attack:
                        player.UnlockAttack();
                        break;
                }

                // 4. Spawn a visual effect if you assigned one
                if (collectEffectPrefab != null)
                {
                    Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
                }

                // 5. Destroy the item so it can't be collected twice
                Destroy(gameObject);
            }
        }
    }
}