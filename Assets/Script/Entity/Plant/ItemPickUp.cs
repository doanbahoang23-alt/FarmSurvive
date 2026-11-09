using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickUp : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private bool isCoin = false;
    [SerializeField] private int coinAmount = 1;

    [Header("Item Settings")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;

    [Header("Magnet Settings")]
    [SerializeField] private bool useMagnetEffect = true;
    [SerializeField] private float magnetSpeed = 8f;
    [SerializeField] private float collectDistance = 0.5f;

    private Transform playerTransform;
    private PlayerInventory playerInventory;
    private bool isFlyingToPlayer = false;

    private void OnEnable()
    {
        isFlyingToPlayer = false;
        playerInventory = null;
        playerTransform = null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerInventory inventory = collision.GetComponentInParent<PlayerInventory>();

        if (!isFlyingToPlayer && inventory != null)
        {
            playerInventory = inventory;
            playerTransform = inventory.transform;

            if (useMagnetEffect)
            {
                isFlyingToPlayer = true;
            }
            else
            {
                CollectItem();
            }
        }
    }

    private void Update()
    {
        if (isFlyingToPlayer && playerTransform != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, magnetSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, playerTransform.position) <= collectDistance)
            {
                CollectItem();
            }
        }
    }

    public void SetCoinAmount(int value)
    {
        isCoin = true;
        coinAmount = value;
    }

    private void CollectItem()
    {
        if (playerInventory != null)
        {
            if (isCoin)
            {
                playerInventory.AddCoins(coinAmount);

                if (AudioManager.Instance != null)
                {
                    AudioClip clip = AudioManager.Instance.coinPickUpClip != null ? AudioManager.Instance.coinPickUpClip : AudioManager.Instance.pickUpClip;
                    if (clip != null) AudioManager.Instance.PlaySFX(clip);
                }

                PoolManager.Instance.Release(gameObject);
            }
            else if (itemData != null)
            {
                playerInventory.AddItem(itemData, amount);

                if (AudioManager.Instance != null && AudioManager.Instance.pickUpClip != null)
                {
                    AudioManager.Instance.PlaySFX(AudioManager.Instance.pickUpClip);
                }

                PoolManager.Instance.Release(gameObject);
            }
        }
    }
}