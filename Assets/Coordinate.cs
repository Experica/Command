using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VLab
{
    public class Coordinate : MonoBehaviour
    {
        public LineRenderer x, y;



        public Vector3 Center
        {
            get { return new Vector3(y.GetPosition(0).x,x.GetPosition(0).y,transform.position.z); }
            set
            {
                for(var i=0;i<x.numPositions;i++)
                {
                    var p = x.GetPosition(i);
                    x.SetPosition(i, new Vector3(p.x,value.y,0));
                }
                for (var i = 0; i < y.numPositions; i++)
                {
                    var p = y.GetPosition(i);
                    y.SetPosition(i, new Vector3(value.x, p.y, 0));
                }
                transform.position = new Vector3(transform.position.x, transform.position.y, value.z);
            }
        }

    }
}
