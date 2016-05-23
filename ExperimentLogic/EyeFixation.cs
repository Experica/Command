// Real-Time Task Control requires checking States constantly, often in a loop.

// Basic Fixation Control
class Fixation
{
    int TaskState;
    double PreITIOnTime, FixOnTime, FixTargetOnTime, SufITIOnTime;


    Fixation()
    {
        // Initial State of Task
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
                else if (FixHold() >= FixDur)
                {
                    // Successfully hold fixation in required period.
                    TurnOffFixTarget();
                    Reward();
                    EnterTaskState(SUFITI);
                    return TASKTRIAL_HIT;
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