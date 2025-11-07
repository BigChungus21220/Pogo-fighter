using Assets;
using UnityEngine;

public class GroundCollisionDetecter : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            RLBalancingAgent balanceA = GetComponentInParent<RLBalancingAgent>();

            if (balanceA != null)
            {
                balanceA.OnFall();
                return;
            }

            RLBouncingAgent bounceA = GetComponentInParent<RLBouncingAgent>();

            if (bounceA != null)
            {
                bounceA.OnFall();
                return;
            }
        }
    }
}
