using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WinDetection : MonoBehaviour {

    [SerializeField] private Image winScreen;
    
    void OnTriggerEnter(Collider other)
    {
        winScreen.gameObject.SetActive(true);
    }
}
