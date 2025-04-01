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

    [SerializeField]
    private HandPosPredictor _posPredictor;

    void Start()
    {
        if(_posPredictor != null)
            _posPredictor.GestureReturnAction += SelectHangeulObj;
    }


    void Update()
    {
        
    }

    private void SelectHangeulObj(string objName)
    {

    }


    public void CreateHangeulObject(GameObject hangeulObj)
    {
        if(_dropSpots != null && _hangeulObjects != null)
        {
            int randomSpot = Random.Range(0, _dropSpots.Count);
            float randomScale = Random.Range(0.5f, 1f);
            GameObject obj = Instantiate(hangeulObj, _dropSpots[randomSpot].transform.position, _dropSpots[randomSpot].transform.rotation);
            obj.transform.localScale = new Vector3(randomScale, randomScale,1);

        }
    }
}
