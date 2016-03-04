using UnityEngine;
using System.Collections;
public class exlogictest : ExperimentLogic {
    // Use this for initialization
    public override void Init() {
        ex.conddur =0.1;
        ex.preICI = 0.1;
        ex.sufICI = 0.1;
        timer.Start();
    }
    public override void Logic()   {
        switch(CondState)
        {
            case CONDSTATE.CONDNONE:
                envmanager.figure.visible = false;
                CondState = CONDSTATE.PREICI;
                break;
            case CONDSTATE.PREICI:
                if (timer.ElapsedSeconds - PreICIOnTime >= ex.preICI)
                {
                    envmanager.figure.visible = true;
                    CondState = CONDSTATE.COND;
                }
                
                break;
            case CONDSTATE.COND:
                if (timer.ElapsedSeconds - CondOnTime >= ex.conddur)
                {
                    envmanager.figure.visible = false;
                    CondState = CONDSTATE.SUFICI;
                }
                break;
            case CONDSTATE.SUFICI:
                if (timer.ElapsedSeconds - SufICIOnTime >= ex.sufICI)
                {
                    CondState = CONDSTATE.PREICI;
                }
                break;
        }

        //if (!experiment.timer.IsRunning)
        //{
        //    experiment.timer.Start();
        //}
        //if ((experiment.timer.ElapsedSeconds - ontime) > experiment.trialdur)
        //{
        //    if (visualobject)
        //    {
        //        var p = visualobject.transform.position;
        //        p.x = (Random.value * 2 - 1) * 10;
        //        p.y = (Random.value * 2 - 1) * 10;
        //        visualobject.position = p;
        //        ontime = experiment.timer.ElapsedSeconds;
        //    }
        //}
    }
    
}
