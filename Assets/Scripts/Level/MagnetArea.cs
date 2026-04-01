using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetArea : MonoBehaviour
{
    public Collider magnetCollider;


    void OnTriggerEnter(Collider other)
    {
        // Tag判断
        if (other.CompareTag("ExperienceOrb")) 
        {
            //Debug.Log("经验球进入磁铁范围");
            var orb = other.GetComponent<ExperienceOrb>();
            if (orb != null)
            {
                orb.Magnetize(this.transform);
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
