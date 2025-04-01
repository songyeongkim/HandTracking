using UnityEngine;

public class DropObjInfo : MonoBehaviour
{
    [SerializeField]
    private string _gestureName;

    public DropObjInfo ReturnThisPrefab(string name)
    {
        Debug.Log(name + ", " +  _gestureName);

        if(name == _gestureName)
        {
            return this;
            Debug.Log("findObj");
        }

        return null;
    }
}
