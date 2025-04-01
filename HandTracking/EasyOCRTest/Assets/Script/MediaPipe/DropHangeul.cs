using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DropHangeul : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> _dropSpots;

    [SerializeField]
    private List<GameObject> _hangeulObjects;

    void Start()
    {
        
    }


    void Update()
    {
        
    }


    public void CreateRandomObject()
    {
        if(_dropSpots != null && _hangeulObjects != null)
        {
            Debug.Log("create");
            int randomSpot = Random.Range(0, _dropSpots.Count);
            int randomObj = Random.Range(0, _hangeulObjects.Count);
            float randomScale = Random.Range(0.5f, 1f);
            GameObject hangeulObj = Instantiate(_hangeulObjects[randomObj], _dropSpots[randomSpot].transform.position, _dropSpots[randomSpot].transform.rotation);
            hangeulObj.transform.localScale = new Vector3(randomScale, randomScale,1);

        }
    }
}
