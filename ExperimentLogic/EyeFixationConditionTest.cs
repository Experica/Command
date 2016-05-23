// Real-Time Task Control requires checking States constantly, often in a loop.

// Condition tests embeded in Fixation Period
class FixationConditionTest
{
    int TaskState, CondState;
    double PreITIOnTime, FixOnTime, FixTargetOnTime, SufITIOnTime;
    double PreICIOnTime, CondOnTime, SufICIOnTime;


    FixationConditionTest()
    {
        // Initial State of Task and Condition Tests
        EnterTaskState(PREITI);
    }

    double PreITIHold()
    {
        return CurrentTime() - PreITIOnTime;
    }
    double FixHold()
    {
        return CurrentTime() - FixOnTime;
    }
    double WaitForFix()
    {
        return CurrentTime() - FixTargetOnTime;
    }
    double PreICIHold()
    {
        return CurrentTime() - PreICIOnTime;
    }
    double CondHold()
    {
        return CurrentTime() - CondOnTime;
    }
    double SufICIHold()
    {
        return CurrentTime() - SufICIOnTime;
    }
    double SufITIHold()
    {
        return CurrentTime() - SufITIOnTime;
    }

    void EnterTaskState(int nexttaskstate)
    {
        // Action when enter next task state
        switch (nexttaskstate)
        {
            case PREITI:
                PreITIDur = RandPreITIDur();
                CondState = COND_NONE;
                PreITIOnTime = CurrentTime();
            case FIXTARGET_ON:
                TurnOnFixTarget();
                FixTargetOnTime = CurrentTime();
            case FIX_ACQUIRED:
                FixDur = RandFixDur();
                FixOnTime = CurrentTime();
            case SUFITI:
                SufITIDur = RandSufITIDur();
                SufITIOnTime = CurrentTime();
        }
        TaskState = nexttaskstate;
    }
    void EnterCondState(int nextcondstate)
    {
        // Action when enter next condition state
        switch (nextcondstate)
        {
            case PREICI:
                PreICIDur = RandPreICIDur();
                PreICIOnTime = CurrentTime();
            case COND_ON:
                CondDur = RandCondDur();
                TurnOnCondition();
                CondOnTime = CurrentTime();
            case SUFICI:
                SufICIDur = RandSufICIDur();
                TurnOffCondition();
                SufICIOnTime = CurrentTime();
        }
        CondState = nextcondstate;
    }


    // State Machine Updated in a loop
    int TaskControl()
    {
        switch (TaskState)
        {
            case PREITI:
                // Turn on Fixation Target when PREITI expired.
                if (PreITIHold() >= PreITIDur)
                {
                    EnterTaskState(FIXTARGET_ON);
                }
            case FIXTARGET_ON:
                if (FixOnTarget())
                {
                    EnterTaskState(FIX_ACQUIRED);
                }
                else if (WaitForFix() >= WaitForFixTimeOut)
                {
                    // Failed to acquire fixation
                    TurnOffFixTarget();
                    Punish();
                    EnterTaskState(PREITI);
                    return TASKTRIAL_FAIL;
                }
            case FIX_ACQUIRED:
                if (!FixOnTarget())
                {
                    // Fixation breaks in required period.
                    TurnOffFixTarget();
                    Punish();
                    EnterTaskState(SUFITI);
                    return TASKTRIAL_EARLY;
                }
                else
                {
                    switch (CondState)
                    {
                        case COND_NONE:
                            if (FixHold() >= FixPreDur)
                            {
                                EnterCondState(PREICI);
                            }
                        case PREICI:
                            if (FixHold() >= FixDur)
                            {
                                // Successfully hold fixation in required period.
                                TurnOffFixTarget();
                                Reward();
                                EnterTaskState(SUFITI);
                                return TASKTRIAL_HIT;
                            }
                            else if (PreICIHold() >= PreICIDur)
                            {
                                EnterCondState(COND_ON);
                            }
                        case COND_ON:
                            if (CondHold() >= CondDur)
                            {
                                EnterCondState(SUFICI);
                            }
                        case SUFICI:
                            if (SufICIHold() >= SufICIDur)
                            {
                                EnterCondState(PREICI);
                            }
                    }
                }
            case SUFITI:
                if (SufITIHold() >= SufITIDur)
                {
                    EnterTaskState(PREITI);
                }
        }
        return TASKTRIAL_CONTINUE;
    }
}