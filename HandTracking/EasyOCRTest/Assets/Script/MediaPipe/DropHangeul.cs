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

    private DropObjInfo _nowDropObjInfo;

    void Awake()
    {
        if(_posPredictor != null)
            _posPredictor.GestureReturnAction += SelectHangeulObj;
    }

    private void SelectHangeulObj(string objName)
    {
        foreach (GameObject objInfo in _hangeulObjects)
        {
            DropObjInfo dropObjInfo;

            if (objInfo.GetComponent<DropObjInfo>() != null)
            {
                dropObjInfo = objInfo.GetComponent<DropObjInfo>();

                DropObjInfo prefabinfo = dropObjInfo.ReturnThisPrefab(objName);
                if (prefabinfo != null)
                {
                    _nowDropObjInfo = prefabinfo;
                    CreateHangeulObject(_nowDropObjInfo.gameObject);
                    return;
                }
            } 
        }

    }


    private void CreateHangeulObject(GameObject hangeulObj)
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
