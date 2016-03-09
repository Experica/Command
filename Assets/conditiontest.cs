using UnityEngine;
using System.Collections;

public class conditiontest : ExperimentLogic
{
    // Use this for initialization
    public override void Init()
    {
        ex.conddur = 0.5;
        ex.preICI = 0.1;
        ex.sufICI = 0.1;
        timer.Start();
    }
    public override void Logic()
    {
        switch (CondState)
        {
            case CONDSTATE.CONDNONE:
                envmanager.ActiveNetBehavior.visible = false;
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (PreICIHold() >= ex.preICI)
                {
                    envmanager.ActiveNetBehavior.visible = true;
                    CondState = CONDSTATE.COND;
                }
                break;
            case CONDSTATE.COND:
                if (CondHold() >= ex.conddur)
                {
                    envmanager.ActiveNetBehavior.visible = false;
                    CondState = CONDSTATE.SUFICI;
                }
                break;
            case CONDSTATE.SUFICI:
                if (SufICIHold() >= ex.sufICI)
                {
                    CondState = CONDSTATE.PREICI;
                }
                break;
        }



        //        var p = visualobject.transform.position;
        //        p.x = (Random.value * 2 - 1) * 10;
        //        p.y = (Random.value * 2 - 1) * 10;
        //        visualobject.position = p;
        //        ontime = experiment.timer.ElapsedSeconds;



        //GetComponent<Renderer>().material.SetColor("Color", new Color(0, 0, r2, 1));
        //GetComponent<Renderer>().material.SetFloat("t", Time.timeSinceLevelLoad);
        //GetComponent<Renderer>().material.SetFloat("ys", transform.localScale.y);
        //GetComponent<Renderer>().material.SetFloat("sigma", 0.05f);
    }

}
